using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.ReservationFeatures;

public class ReservationConcurrencyTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public ReservationConcurrencyTests(PostgresFixture fixture) => _fixture = fixture;

  private AppDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_fixture.ConnectionString)
      .Options;
    return new AppDbContext(options);
  }

  [Fact]
  public async Task ConcurrentReservations_OnSameSpace_OnlyOneSucceeds()
  {
    await using (var setup = CreateContext())
    {
      await setup.Database.MigrateAsync();
    }

    ParkingSpaceId spaceId;
    ParkingLotId lotId;
    DriverId driverAId;
    DriverId driverBId;

    await using (var seed = CreateContext())
    {
      var lot = ParkingLot.Create("Concurrency Lot", "America/New_York");
      var space = lot.AddSpace("A1", SpaceType.Reserved, actorId: null);
      var driverA = Driver.Create("Driver A", null, actorId: null);
      var driverB = Driver.Create("Driver B", null, actorId: null);

      seed.ParkingLots.Add(lot);
      seed.Drivers.Add(driverA);
      seed.Drivers.Add(driverB);
      await seed.SaveChangesAsync();

      spaceId = space.Id;
      lotId = lot.Id;
      driverAId = driverA.Id;
      driverBId = driverB.Id;
    }

    async Task<bool> TryReserve(DriverId driverId)
    {
      await using var context = CreateContext();
      var reservation = Reservation.Create(spaceId, driverId, lotId, actorId: null);
      context.Reservations.Add(reservation);
      try
      {
        await context.SaveChangesAsync();
        return true;
      }
      catch (DbUpdateException)
      {
        return false;
      }
    }

    var results = await Task.WhenAll(TryReserve(driverAId), TryReserve(driverBId));

    results.Count(succeeded => succeeded).ShouldBe(1);

    await using var verify = CreateContext();
    var activeCount = await verify.Reservations
      .CountAsync(r => r.SpaceId == spaceId && r.Status == ReservationStatus.Active);
    activeCount.ShouldBe(1);
  }
}
