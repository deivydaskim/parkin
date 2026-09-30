using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.Exceptions;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Features.AccessEvents;
using Parkin.Api.Features.AccessEvents.Ingest;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Data.Queries;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.AccessEventFeatures;

public class AccessEventIngestionTests : IClassFixture<PostgresFixture>
{
  private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

  private readonly PostgresFixture _fixture;
  private readonly AccessEventActor _gate = AccessEventActor.ApiKey(Guid.NewGuid());

  public AccessEventIngestionTests(PostgresFixture fixture) => _fixture = fixture;

  private AppDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_fixture.ConnectionString)
      .Options;
    return new AppDbContext(options);
  }

  private static IngestAccessEventHandler CreateHandler(AppDbContext context)
  {
    var gateLotReader = new GateLotReader(context, new LotRowLocker(context));

    return new IngestAccessEventHandler(
      new AccessEventIdempotency(new AccessEventReplayQueryService(context)),
      new EfUnitOfWork(context),
      gateLotReader,
      new EntryContextBuilder(new EfRepository<Driver>(context), new EfRepository<AccessGrant>(context),
        new EfRepository<Reservation>(context), gateLotReader),
      new EfRepository<AccessEvent>(context),
      new EfRepository<ParkingSession>(context),
      new EfRepository<AuditLogEntry>(context),
      new FixedTimeProvider(Now));
  }

  private IngestAccessEventCommand Command(ParkingLotId lotId, string plate, Direction direction,
    string idempotencyKey, AccessEventActor? actor = null) =>
    new(lotId, plate, direction, EventSource.Lpr, Now, idempotencyKey, actor ?? _gate);

  private async Task<Result<AccessEventDecisionResponse>> IngestAsync(IngestAccessEventCommand command)
  {
    await using var context = CreateContext();
    return await CreateHandler(context).Handle(command, CancellationToken.None);
  }

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

    var first = (await IngestAsync(Command(lotId, "IDEM 1", Direction.Enter, "replay-key"))).Value;
    var replay = (await IngestAsync(Command(lotId, "IDEM 1", Direction.Enter, "replay-key"))).Value;

    replay.ShouldBe(first);

    await using var verify = CreateContext();
    (await verify.AccessEvents.CountAsync(e => e.IdempotencyKey == "replay-key")).ShouldBe(1);
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId && s.Status == SessionStatus.Active))
      .ShouldBe(1);
  }

  [Fact]
  public async Task SameIdempotencyKey_FromTwoApiKeys_IsRecordedIndependently()
  {
    var lotId = await SeedLotAsync("Two Gates Lot", generalSpaces: 5, FullBehavior.Block);
    var northGate = AccessEventActor.ApiKey(Guid.NewGuid());
    var southGate = AccessEventActor.ApiKey(Guid.NewGuid());

    var north = (await IngestAsync(Command(lotId, "NORTH 1", Direction.Enter, "1", northGate))).Value;
    var south = (await IngestAsync(Command(lotId, "SOUTH 1", Direction.Enter, "1", southGate))).Value;

    north.Decision.ShouldBe(Decision.Allow);
    south.Decision.ShouldBe(Decision.Allow);
    south.EventId.ShouldNotBe(north.EventId);
    south.SessionId.ShouldNotBe(north.SessionId);

    await using var verify = CreateContext();
    (await verify.AccessEvents.CountAsync(e => e.LotId == lotId && e.IdempotencyKey == "1")).ShouldBe(2);
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId && s.Status == SessionStatus.Active))
      .ShouldBe(2);
  }

  [Fact]
  public async Task ReplayedKeyForADifferentPlate_ConflictsAndRecordsNothing()
  {
    var lotId = await SeedLotAsync("Mismatch Lot", generalSpaces: 5, FullBehavior.Block);

    var first = await IngestAsync(Command(lotId, "MATCH 1", Direction.Enter, "mismatch-key"));
    var reused = await IngestAsync(Command(lotId, "OTHER 1", Direction.Enter, "mismatch-key"));

    first.Status.ShouldBe(ResultStatus.Ok);
    reused.Status.ShouldBe(ResultStatus.Conflict);

    await using var verify = CreateContext();
    (await verify.AccessEvents.CountAsync(e => e.IdempotencyKey == "mismatch-key")).ShouldBe(1);
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId)).ShouldBe(1);
  }

  [Fact]
  public async Task ConcurrentRequests_WithTheSameKey_RecordOneEventAndReturnOneDecision()
  {
    var lotId = await SeedLotAsync("Race Key Lot", generalSpaces: 5, FullBehavior.Block);

    var results = await Task.WhenAll(
      IngestAsync(Command(lotId, "SAME 1", Direction.Enter, "race-key")),
      IngestAsync(Command(lotId, "SAME 1", Direction.Enter, "race-key")));

    results.ShouldAllBe(result => result.Status == ResultStatus.Ok);
    results[1].Value.EventId.ShouldBe(results[0].Value.EventId);

    await using var verify = CreateContext();
    (await verify.AccessEvents.CountAsync(e => e.IdempotencyKey == "race-key")).ShouldBe(1);
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId)).ShouldBe(1);
  }

  [Fact]
  public async Task DuplicateIdempotencyKey_WithinOneScope_IsRejectedByTheUniqueIndex()
  {
    var lotId = await SeedLotAsync("Unique Index Lot", generalSpaces: 1, FullBehavior.Block);

    async Task<bool> TryInsert(AccessEventActor actor)
    {
      await using var context = CreateContext();
      context.AccessEvents.Add(AccessEvent.Record(lotId, "DUP 1", "DUP1", null, null, Direction.Enter,
        EventSource.Lpr, Decision.Allow, denyReason: null, Now, Now, "duplicate-key", actor));
      try
      {
        await context.SaveChangesAsync();
        return true;
      }
      catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == AccessEvent.IdempotencyIndexName)
      {
        return false;
      }
    }

    var sameScope = await Task.WhenAll(TryInsert(_gate), TryInsert(_gate));
    var otherScope = await TryInsert(AccessEventActor.ApiKey(Guid.NewGuid()));

    sameScope.Count(succeeded => succeeded).ShouldBe(1);
    otherScope.ShouldBeTrue();
  }

  [Fact]
  public async Task ConcurrentEntries_AtTheLastFreeSpace_AdmitExactlyOne()
  {
    var lotId = await SeedLotAsync("Concurrency Lot", generalSpaces: 1, FullBehavior.Block);

    var decisions = await Task.WhenAll(
      IngestAsync(Command(lotId, "RACE 1", Direction.Enter, "race-1")),
      IngestAsync(Command(lotId, "RACE 2", Direction.Enter, "race-2")));

    decisions.Count(d => d.Value.Decision == Decision.Allow).ShouldBe(1);
    decisions.Count(d => d.Value.Decision == Decision.Deny && d.Value.Reason == DenyReason.LotFull).ShouldBe(1);

    await using var verify = CreateContext();
    (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId && s.Status == SessionStatus.Active))
      .ShouldBe(1);
  }

  [Fact]
  public async Task EntryAfterTheLotIsArchived_IsDenied()
  {
    var lotId = await SeedLotAsync("Archived Lot", generalSpaces: 2, FullBehavior.Block);
    await using (var archive = CreateContext())
    {
      var lot = await archive.ParkingLots.SingleAsync(l => l.Id == lotId);
      lot.Archive(actorId: null);
      await archive.SaveChangesAsync();
    }

    var decision = (await IngestAsync(Command(lotId, "LATE 1", Direction.Enter, "archived-enter"))).Value;

    decision.Decision.ShouldBe(Decision.Deny);
    decision.Reason.ShouldBe(DenyReason.LotArchived);
  }

  [Fact]
  public async Task ExitWithNoOpenSession_DoesNotDriveOccupancyNegative()
  {
    var lotId = await SeedLotAsync("Anomaly Lot", generalSpaces: 3, FullBehavior.Block);

    var decision = (await IngestAsync(Command(lotId, "GHOST 1", Direction.Exit, "ghost-exit"))).Value;

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

    (await IngestAsync(Command(lotId, "CYCLE 1", Direction.Enter, "cycle-enter"))).Value.Decision
      .ShouldBe(Decision.Allow);
    (await IngestAsync(Command(lotId, "CYCLE 1", Direction.Exit, "cycle-exit"))).Value.Decision
      .ShouldBe(Decision.Allow);

    await using (var verify = CreateContext())
    {
      (await verify.ParkingSessions.CountAsync(s => s.LotId == lotId && s.Status == SessionStatus.Active))
        .ShouldBe(0);
    }

    (await IngestAsync(Command(lotId, "CYCLE 2", Direction.Enter, "cycle-next"))).Value.Decision
      .ShouldBe(Decision.Allow);
  }

  private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow() => now;
  }
}
