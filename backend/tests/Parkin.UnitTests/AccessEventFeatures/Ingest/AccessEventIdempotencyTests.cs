using Ardalis.Result;
using NSubstitute;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Exceptions;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.Features.AccessEvents;
using Parkin.Api.Features.AccessEvents.Ingest;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.AccessEventFeatures.Ingest;

public class AccessEventIdempotencyTests
{
  private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

  private readonly IAccessEventReplayQueryService _replayQuery = Substitute.For<IAccessEventReplayQueryService>();
  private readonly AccessEventActor _actor = AccessEventActor.ApiKey(Guid.NewGuid());
  private readonly ParkingLotId _lotId = ParkingLotId.From(Guid.NewGuid());

  private readonly AccessEventDecisionResponse _original = new(Guid.NewGuid(), Decision.Allow, null,
    SessionPool.General, null, Guid.NewGuid(), Now);

  private readonly AccessEventDecisionResponse _fresh = new(Guid.NewGuid(), Decision.Deny, DenyReason.LotFull,
    null, null, null, Now);

  private int _ingestCalls;

  public AccessEventIdempotencyTests() =>
    _replayQuery.FindAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns((RecordedAccessEvent?)null);

  private AccessEventIdempotency CreateSut() => new(_replayQuery);

  private IngestAccessEventCommand Command(string plate = "aaa 111", Direction direction = Direction.Enter,
    ParkingLotId? lotId = null) =>
    new(lotId ?? _lotId, plate, direction, EventSource.Lpr, Now, "key-1", _actor);

  private Task<Result<AccessEventDecisionResponse>> Ingest(CancellationToken _)
  {
    _ingestCalls++;
    return Task.FromResult(Result.Success(_fresh));
  }

  private void GivenRecorded(ParkingLotId lotId, string normalizedPlate, Direction direction) =>
    _replayQuery.FindAsync(_actor.Id, "key-1", Arg.Any<CancellationToken>())
      .Returns(new RecordedAccessEvent(lotId, normalizedPlate, direction, _original));

  [Fact]
  public async Task RunOnce_NoRecordedEvent_RunsTheIngest()
  {
    var result = await CreateSut().RunOnceAsync(Command(), Ingest, CancellationToken.None);

    result.Value.ShouldBe(_fresh);
    _ingestCalls.ShouldBe(1);
  }

  [Fact]
  public async Task RunOnce_SameEventReplayed_ReturnsOriginalWithoutIngesting()
  {
    GivenRecorded(_lotId, "AAA111", Direction.Enter);

    var result = await CreateSut().RunOnceAsync(Command(), Ingest, CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.ShouldBe(_original);
    _ingestCalls.ShouldBe(0);
  }

  [Fact]
  public async Task RunOnce_KeyReusedForAnotherLot_Conflicts()
  {
    GivenRecorded(ParkingLotId.From(Guid.NewGuid()), "AAA111", Direction.Enter);

    var result = await CreateSut().RunOnceAsync(Command(), Ingest, CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    result.Errors.ShouldContain(AccessEventIdempotency.KeyReusedMessage);
    _ingestCalls.ShouldBe(0);
  }

  [Fact]
  public async Task RunOnce_KeyReusedForAnotherPlate_Conflicts()
  {
    GivenRecorded(_lotId, "BBB222", Direction.Enter);

    var result = await CreateSut().RunOnceAsync(Command(), Ingest, CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    _ingestCalls.ShouldBe(0);
  }

  [Fact]
  public async Task RunOnce_KeyReusedForTheOtherDirection_Conflicts()
  {
    GivenRecorded(_lotId, "AAA111", Direction.Enter);

    var result = await CreateSut().RunOnceAsync(Command(direction: Direction.Exit), Ingest, CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    _ingestCalls.ShouldBe(0);
  }

  [Fact]
  public async Task RunOnce_LooksUpOnlyTheCallersScope()
  {
    await CreateSut().RunOnceAsync(Command(), Ingest, CancellationToken.None);

    await _replayQuery.Received(1).FindAsync(_actor.Id, "key-1", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RunOnce_LosesTheRaceForTheKey_ReturnsTheWinnersDecision()
  {
    _replayQuery.FindAsync(_actor.Id, "key-1", Arg.Any<CancellationToken>())
      .Returns(
        (RecordedAccessEvent?)null,
        new RecordedAccessEvent(_lotId, "AAA111", Direction.Enter, _original));

    var result = await CreateSut().RunOnceAsync(Command(),
      _ => throw new UniqueConstraintViolationException(AccessEvent.IdempotencyIndexName, new Exception()),
      CancellationToken.None);

    result.Value.ShouldBe(_original);
  }

  [Fact]
  public async Task RunOnce_LosesTheRaceToADifferentEvent_Conflicts()
  {
    _replayQuery.FindAsync(_actor.Id, "key-1", Arg.Any<CancellationToken>())
      .Returns(
        (RecordedAccessEvent?)null,
        new RecordedAccessEvent(_lotId, "OTHER1", Direction.Enter, _original));

    var result = await CreateSut().RunOnceAsync(Command(),
      _ => throw new UniqueConstraintViolationException(AccessEvent.IdempotencyIndexName, new Exception()),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
  }

  [Fact]
  public async Task RunOnce_OtherUniqueViolation_Rethrows()
  {
    await Should.ThrowAsync<UniqueConstraintViolationException>(() => CreateSut().RunOnceAsync(Command(),
      _ => throw new UniqueConstraintViolationException("some_other_index", new Exception()),
      CancellationToken.None));
  }
}
