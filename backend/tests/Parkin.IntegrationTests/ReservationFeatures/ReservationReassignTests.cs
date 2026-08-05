using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.ReservationFeatures;

// Proves the atomic-swap guarantee required by T4.2 against a real Postgres:
//  1. the naive "wrong statement order" really does trip the partial unique index
//     (ux_reservation_active_space), so the trap described in the task is real;
//  2. the actual EfUnitOfWork-based mechanism (cancel+save, then create+save, one
//     transaction) never trips it and always leaves exactly one ACTIVE row;
//  3. a second, independent connection never observes zero ACTIVE rows for the space
//     while the swap's transaction is still open — Postgres MVCC hides the uncommitted
//     cancel from everyone else until commit.
public class ReservationReassignTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public ReservationReassignTests(PostgresFixture fixture) => _fixture = fixture;

  private AppDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_fixture.ConnectionString)
      .Options;
    return new AppDbContext(options);
  }

  private async Task<(ParkingSpaceId SpaceId, ParkingLotId LotId, DriverId OldDriverId, DriverId NewDriverId, ReservationId OldReservationId)> SeedAsync()
  {
    await using (var setup = CreateContext())
    {
      await setup.Database.MigrateAsync();
    }

    await using var seed = CreateContext();
    // Unique per call: PostgresFixture (and its Postgres container/data) is shared across
    // every [Fact] in this class via IClassFixture, so a fixed name would collide with
    // ux_lot_name on the second test to run.
    var lot = ParkingLot.Create($"Reassign Lot {Guid.NewGuid()}", "America/New_York");
    var space = lot.AddSpace("A1", SpaceType.Reserved, actorId: null);
    var oldDriver = Driver.Create("Old Driver", null, actorId: null);
    var newDriver = Driver.Create("New Driver", null, actorId: null);

    seed.ParkingLots.Add(lot);
    seed.Drivers.Add(oldDriver);
    seed.Drivers.Add(newDriver);
    await seed.SaveChangesAsync();

    var reservation = Reservation.Create(space.Id, oldDriver.Id, lot.Id, actorId: null);
    seed.Reservations.Add(reservation);
    await seed.SaveChangesAsync();

    return (space.Id, lot.Id, oldDriver.Id, newDriver.Id, reservation.Id);
  }

  [Fact]
  public async Task WrongStatementOrder_InsertBeforeCancel_ThrowsUniqueViolation()
  {
    // Sanity check that the trap called out in T4.2 is real: if the INSERT of the new
    // ACTIVE reservation runs before the old row is Cancelled — even inside one
    // transaction — ux_reservation_active_space sees two ACTIVE rows for the space and
    // Postgres rejects it.
    var (spaceId, lotId, oldDriverId, newDriverId, oldReservationId) = await SeedAsync();

    await using var context = CreateContext();
    await using var transaction = await context.Database.BeginTransactionAsync();

    var newReservation = Reservation.CreateForReassignment(
      spaceId, newDriverId, lotId, oldReservationId, oldDriverId, actorId: null);
    context.Reservations.Add(newReservation);

    await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task Reassign_UsingUnitOfWork_EndsOldAndActivatesNew_AsSingleAtomicSwap()
  {
    var (spaceId, lotId, oldDriverId, newDriverId, oldReservationId) = await SeedAsync();

    await using var context = CreateContext();
    var oldReservation = await context.Reservations.SingleAsync(r => r.Id == oldReservationId);
    var unitOfWork = new EfUnitOfWork(context);

    // This is the exact statement order ReassignReservationHandler uses: cancel+save,
    // then create+save, both inside one ambient transaction opened by EfUnitOfWork.
    await unitOfWork.ExecuteInTransactionAsync(async ct =>
    {
      oldReservation.Cancel(actorId: null);
      await context.SaveChangesAsync(ct);

      var newReservation = Reservation.CreateForReassignment(
        spaceId, newDriverId, lotId, oldReservationId, oldDriverId, actorId: null);
      context.Reservations.Add(newReservation);
      await context.SaveChangesAsync(ct);
    }, CancellationToken.None);

    await using var verify = CreateContext();
    var reservationsForSpace = await verify.Reservations.Where(r => r.SpaceId == spaceId).ToListAsync();

    reservationsForSpace.Count.ShouldBe(2);

    var cancelled = reservationsForSpace.Single(r => r.Id == oldReservationId);
    cancelled.Status.ShouldBe(ReservationStatus.Cancelled);
    cancelled.DriverId.ShouldBe(oldDriverId);

    var active = reservationsForSpace.Single(r => r.Status == ReservationStatus.Active);
    active.Id.ShouldNotBe(oldReservationId);
    active.DriverId.ShouldBe(newDriverId);

    // Exactly one ACTIVE row for the space — the invariant ux_reservation_active_space enforces.
    reservationsForSpace.Count(r => r.Status == ReservationStatus.Active).ShouldBe(1);
  }

  [Fact]
  public async Task Reassign_WhileTransactionIsOpen_OtherConnectionNeverSeesZeroActiveReservations()
  {
    var (spaceId, lotId, oldDriverId, newDriverId, oldReservationId) = await SeedAsync();

    await using var swapContext = CreateContext();
    var oldReservation = await swapContext.Reservations.SingleAsync(r => r.Id == oldReservationId);
    await using var transaction = await swapContext.Database.BeginTransactionAsync();

    // Step 1 only: cancel + save, but do NOT commit yet — simulates being paused
    // mid-way through EfUnitOfWork.ExecuteInTransactionAsync.
    oldReservation.Cancel(actorId: null);
    await swapContext.SaveChangesAsync();

    // A completely separate connection must still see the space as actively reserved
    // by the OLD driver — the uncommitted cancel is invisible outside this transaction.
    await using (var otherConnection = CreateContext())
    {
      var visibleToOthers = await otherConnection.Reservations
        .Where(r => r.SpaceId == spaceId && r.Status == ReservationStatus.Active)
        .ToListAsync();

      visibleToOthers.Count.ShouldBe(1);
      visibleToOthers.Single().Id.ShouldBe(oldReservationId);
    }

    // Step 2: create the new ACTIVE reservation + save, then commit.
    var newReservation = Reservation.CreateForReassignment(
      spaceId, newDriverId, lotId, oldReservationId, oldDriverId, actorId: null);
    swapContext.Reservations.Add(newReservation);
    await swapContext.SaveChangesAsync();
    await transaction.CommitAsync();

    await using var verify = CreateContext();
    var afterCommit = await verify.Reservations
      .Where(r => r.SpaceId == spaceId && r.Status == ReservationStatus.Active)
      .ToListAsync();

    afterCommit.Count.ShouldBe(1);
    afterCommit.Single().DriverId.ShouldBe(newDriverId);
  }
}
