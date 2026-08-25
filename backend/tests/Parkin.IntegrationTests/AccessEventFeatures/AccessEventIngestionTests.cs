using Microsoft.EntityFrameworkCore;
using Parkin.Api.AccessEventFeatures;
using Parkin.Api.AccessEventFeatures.Ingest;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Data.Queries;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.AccessEventFeatures;

public class AccessEventIngestionTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public AccessEventIngestionTests(PostgresFixture fixture) => _fixture = fixture;

  private AppDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_fixture.ConnectionString)
      .Options;
    return new AppDbContext(options);
  }

  private static IngestAccessEventHandler CreateHandler(AppDbContext context) => new(
    new EfRepository<AccessEvent>(context),
    new EfRepository<ParkingSession>(context),
    new EfRepository<AuditLogEntry>(context),
    new EfRepository<ParkingLot>(context),
    new EfRepository<Driver>(context),
    new EfRepository<AccessGrant>(context),
    new EfRepository<Reservation>(context),
    new EntryDecisionService(),
    new OccupancyCalculator(),
    new LotRowLocker(context),
    new EfUnitOfWork(context));

  private static IngestAccessEventCommand Command(ParkingLotId lotId, string plate, Direction direction,
    string idempotencyKey) =>
    new(lotId, plate, direction, EventSource.Lpr, DateTimeOffset.UtcNow, idempotencyKey, ActorId: null);

  private async Task<ParkingLotId> SeedLotAsync(string name, int generalSpaces, FullBehavior fullBehavior)
  {
    await using (var setup = CreateContext())
    {
      await setup.Database.MigrateAsync();
    }

    await using var seed = CreateContext();
    var lot = ParkingLot.Create(name, "Europe/Vilnius", fullBehavior: fullBehavior);
    for (var i = 0; i < generalSpaces; i++)
    {
      lot.AddSpace($"G{i}", SpaceType.General, actorId: null);
    }

    seed.ParkingLots.Add(lot);
    await seed.SaveChangesAsync();
    return lot.Id;
  }

  [Fact]
  public async Task ReplayedIdempotencyKey_ReturnsOriginalAndDoesNotDoubleCount()
  {
    var lotId = await SeedLotAsync("Idempotency Lot", generalSpaces: 5, FullBehavior.Block);

    AccessEventDecisionDto first;
    await using (var context = CreateContext())
    {
      first = (await CreateHandler(context).Handle(
        Command(lotId, "IDEM 1", Direction.Enter, "replay-key"), CancellationToken.None)).Value;
    }

    AccessEventDecisionDto replay;
    await using (var context = CreateContext())
    {
      replay = (await CreateHandler(context).Handle(
        Command(lotId, "IDEM 1", Direction.Enter, "replay-key"), CancellationToken.None)).Value;
    }

    replay.EventId.ShouldBe(first.EventId);
    replay.Decision.ShouldBe(first.Decision);
    replay.SessionId.ShouldBe(first.SessionId);

    await using var verify = CreateContext();
    (await verify.AccessEvents.CountAsync(e => e.IdempotencyKey == "replay-key")).ShouldBe(1);
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId && s.Status == SessionStatus.Active))
      .ShouldBe(1);
  }

  [Fact]
  public async Task DuplicateIdempotencyKey_IsRejectedByTheUniqueIndex()
  {
    var lotId = await SeedLotAsync("Unique Index Lot", generalSpaces: 1, FullBehavior.Block);

    async Task<bool> TryInsert()
    {
      await using var context = CreateContext();
      context.AccessEvents.Add(AccessEvent.Record(lotId, "DUP 1", "DUP1", null, null, Direction.Enter,
        EventSource.Lpr, Decision.Allow, denyReason: null, DateTimeOffset.UtcNow, "duplicate-key",
        actorId: null));
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

    var results = await Task.WhenAll(TryInsert(), TryInsert());

    results.Count(succeeded => succeeded).ShouldBe(1);
  }

  [Fact]
  public async Task ConcurrentEntries_AtTheLastFreeSpace_AdmitExactlyOne()
  {
    var lotId = await SeedLotAsync("Concurrency Lot", generalSpaces: 1, FullBehavior.Block);

    async Task<AccessEventDecisionDto> Enter(string plate, string key)
    {
      await using var context = CreateContext();
      return (await CreateHandler(context).Handle(
        Command(lotId, plate, Direction.Enter, key), CancellationToken.None)).Value;
    }

    var decisions = await Task.WhenAll(
      Enter("RACE 1", "race-1"),
      Enter("RACE 2", "race-2"));

    // Without the per-lot FOR UPDATE both would read "one space free" and both be admitted.
    decisions.Count(d => d.Decision == Decision.Allow).ShouldBe(1);
    decisions.Count(d => d.Decision == Decision.Deny && d.Reason == DenyReason.LotFull).ShouldBe(1);

    await using var verify = CreateContext();
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId && s.Status == SessionStatus.Active))
      .ShouldBe(1);
  }

  [Fact]
  public async Task ExitWithNoOpenSession_DoesNotDriveOccupancyNegative()
  {
    var lotId = await SeedLotAsync("Anomaly Lot", generalSpaces: 3, FullBehavior.Block);

    AccessEventDecisionDto decision;
    await using (var context = CreateContext())
    {
      decision = (await CreateHandler(context).Handle(
        Command(lotId, "GHOST 1", Direction.Exit, "ghost-exit"), CancellationToken.None)).Value;
    }

    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.NoOpenSession);

    await using var verify = CreateContext();
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId)).ShouldBe(0);
    (await verify.AccessEvents.CountAsync(e => e.IdempotencyKey == "ghost-exit")).ShouldBe(1);
  }

  [Fact]
  public async Task EnterThenExit_ClosesTheSessionAndFreesTheSpace()
  {
    var lotId = await SeedLotAsync("Lifecycle Lot", generalSpaces: 1, FullBehavior.Block);

    await using (var context = CreateContext())
    {
      var entered = (await CreateHandler(context).Handle(
        Command(lotId, "CYCLE 1", Direction.Enter, "cycle-enter"), CancellationToken.None)).Value;
      entered.Decision.ShouldBe(Decision.Allow);
    }

    await using (var context = CreateContext())
    {
      var exited = (await CreateHandler(context).Handle(
        Command(lotId, "CYCLE 1", Direction.Exit, "cycle-exit"), CancellationToken.None)).Value;
      exited.Decision.ShouldBe(Decision.Allow);
    }

    await using (var verify = CreateContext())
    {
      (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId && s.Status == SessionStatus.Active))
        .ShouldBe(0);
    }

    await using (var context = CreateContext())
    {
      var next = (await CreateHandler(context).Handle(
        Command(lotId, "CYCLE 2", Direction.Enter, "cycle-next"), CancellationToken.None)).Value;
      next.Decision.ShouldBe(Decision.Allow);
    }
  }
}
