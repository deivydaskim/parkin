using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate.Specifications;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.Domain.Services;
using Parkin.Api.Features.AccessEvents;
using Parkin.Api.Features.AccessEvents.Ingest;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.AccessEventFeatures.Ingest;

public class IngestAccessEventHandlerTests
{
  private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);
  private static readonly DateTimeOffset ReceivedAt = Now.AddSeconds(2);

  private readonly IAccessEventReplayQueryService _replayQuery = Substitute.For<IAccessEventReplayQueryService>();
  private readonly IGateLotReader _gateLotReader = Substitute.For<IGateLotReader>();
  private readonly IRepository<AccessEvent> _accessEventRepository = Substitute.For<IRepository<AccessEvent>>();
  private readonly IRepository<ParkingSession> _sessionRepository = Substitute.For<IRepository<ParkingSession>>();
  private readonly IRepository<AuditLogEntry> _auditRepository = Substitute.For<IRepository<AuditLogEntry>>();
  private readonly IReadRepository<Driver> _driverRepository = Substitute.For<IReadRepository<Driver>>();
  private readonly IReadRepository<AccessGrant> _grantRepository = Substitute.For<IReadRepository<AccessGrant>>();
  private readonly IReadRepository<Reservation> _reservationRepository = Substitute.For<IReadRepository<Reservation>>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

  private readonly AccessEventActor _gate = AccessEventActor.ApiKey(Guid.NewGuid());
  private readonly ParkingLotId _lotId = ParkingLotId.From(Guid.NewGuid());

  public IngestAccessEventHandlerTests()
  {
    _unitOfWork
      .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
      .Returns(callInfo => ((Func<CancellationToken, Task>)callInfo[0])(CancellationToken.None));

    _timeProvider.GetUtcNow().Returns(ReceivedAt);

    _replayQuery.FindAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns((RecordedAccessEvent?)null);
    _driverRepository
      .FirstOrDefaultAsync(Arg.Any<PlateByNormalizedValueSpec>(), Arg.Any<CancellationToken>())
      .Returns((Driver?)null);
    _reservationRepository
      .FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns((Reservation?)null);
    _grantRepository
      .ListAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns([]);
    _sessionRepository
      .FirstOrDefaultAsync(Arg.Any<MostRecentActiveSessionByPlateSpec>(), Arg.Any<CancellationToken>())
      .Returns((ParkingSession?)null);
  }

  private IngestAccessEventHandler CreateSut() => new(
    new AccessEventIdempotency(_replayQuery),
    _unitOfWork,
    _gateLotReader,
    new EntryContextBuilder(_driverRepository, _grantRepository, _reservationRepository, _gateLotReader),
    _accessEventRepository,
    _sessionRepository,
    _auditRepository,
    _timeProvider);

  private IngestAccessEventCommand Command(string plate = "AAA 111", Direction direction = Direction.Enter,
    AccessEventActor? actor = null, EventSource source = EventSource.Lpr) =>
    new(_lotId, plate, direction, source, Now, "key-1", actor ?? _gate);

  private void GivenLot(AccessMode accessMode = AccessMode.Open, FullBehavior fullBehavior = FullBehavior.Block,
    int generalCapacity = 5, int activeGeneralSessions = 0, LotStatus status = LotStatus.Active) =>
    _gateLotReader.LockAndLoadAsync(_lotId, Arg.Any<CancellationToken>())
      .Returns(new GateLot(_lotId, status, accessMode, fullBehavior, generalCapacity, activeGeneralSessions, 0));

  private void GivenNoLot() =>
    _gateLotReader.LockAndLoadAsync(_lotId, Arg.Any<CancellationToken>()).Returns((GateLot?)null);

  private Driver GivenKnownDriver(string plate = "AAA 111")
  {
    var driver = Driver.Create("Jane Driver", null, actorId: null);
    driver.AddPlate(plate, actorId: null);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<PlateByNormalizedValueSpec>(), Arg.Any<CancellationToken>())
      .Returns(driver);
    return driver;
  }

  [Fact]
  public async Task Handle_ReplayOfTheSameEvent_ReturnsOriginalAndWritesNothing()
  {
    var original = new AccessEventDecisionResponse(Guid.NewGuid(), Decision.Deny, DenyReason.LotFull, null, null,
      null, Now.AddMinutes(-1));
    _replayQuery.FindAsync(_gate.Id, "key-1", Arg.Any<CancellationToken>())
      .Returns(new RecordedAccessEvent(_lotId, "AAA111", Direction.Enter, original));

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.ShouldBe(original);
    await _accessEventRepository.DidNotReceive().AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
      Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_KeyReusedForADifferentPlate_ReturnsConflictAndWritesNothing()
  {
    var original = new AccessEventDecisionResponse(Guid.NewGuid(), Decision.Allow, null, SessionPool.General, null,
      Guid.NewGuid(), Now);
    _replayQuery.FindAsync(_gate.Id, "key-1", Arg.Any<CancellationToken>())
      .Returns(new RecordedAccessEvent(_lotId, "OTHER1", Direction.Enter, original));

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
      Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_LooksUpReplaysWithinTheCallersScope()
  {
    GivenLot();

    await CreateSut().Handle(Command(), CancellationToken.None);

    await _replayQuery.Received().FindAsync(_gate.Id, "key-1", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_UnknownLotFromTheGate_DeniesAndAuditsWithoutPersistingAnEvent()
  {
    GivenNoLot();

    AuditLogEntry? audited = null;
    await _auditRepository.AddAsync(Arg.Do<AuditLogEntry>(e => audited = e), Arg.Any<CancellationToken>());

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotNotFound);
    result.Value.EventId.ShouldBeNull();
    await _accessEventRepository.DidNotReceive().AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());

    audited.ShouldNotBeNull();
    audited.ActorType.ShouldBe(AuditActorType.Api);
    audited.ActorId.ShouldBe(_gate.Id);
    audited.OccurredAt.ShouldBe(ReceivedAt);
  }

  [Fact]
  public async Task Handle_UnknownLotFromStaff_ReturnsNotFoundAndAuditsNothing()
  {
    GivenNoLot();

    var result = await CreateSut().Handle(
      Command(actor: AccessEventActor.Staff(Guid.NewGuid()), source: EventSource.Manual), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
    await _auditRepository.DidNotReceive().AddAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
    await _accessEventRepository.DidNotReceive().AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ArchivedLot_DeniesAndPersistsTheEvent()
  {
    GivenLot(status: LotStatus.Archived);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotArchived);
    result.Value.EventId.ShouldNotBeNull();
    await _accessEventRepository.Received(1).AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());
    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_EnterAllowedOnOpenLot_OpensGeneralSessionWithNoSpace()
  {
    GivenLot();

    ParkingSession? opened = null;
    await _sessionRepository.AddAsync(Arg.Do<ParkingSession>(s => opened = s), Arg.Any<CancellationToken>());

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    result.Value.Pool.ShouldBe(SessionPool.General);
    result.Value.ReservedSpaceLabel.ShouldBeNull();
    result.Value.SessionId.ShouldNotBeNull();

    opened.ShouldNotBeNull();
    opened.Pool.ShouldBe(SessionPool.General);
    opened.SpaceId.ShouldBeNull();
    opened.DriverId.ShouldBeNull();
    opened.Plate.ShouldBe("AAA111");
    opened.Status.ShouldBe(SessionStatus.Active);
    opened.EntryTime.ShouldBe(Now);
  }

  [Fact]
  public async Task Handle_RecordedEvent_CarriesScopeAndReceivedAtFromTheTimeProvider()
  {
    GivenLot();

    AccessEvent? recorded = null;
    await _accessEventRepository.AddAsync(Arg.Do<AccessEvent>(e => recorded = e), Arg.Any<CancellationToken>());

    await CreateSut().Handle(Command(), CancellationToken.None);

    recorded.ShouldNotBeNull();
    recorded.ActorId.ShouldBe(_gate.Id);
    recorded.ActingStaffId.ShouldBeNull();
    recorded.IdempotencyKey.ShouldBe("key-1");
    recorded.OccurredAt.ShouldBe(Now);
    recorded.ReceivedAt.ShouldBe(ReceivedAt);
  }

  [Fact]
  public async Task Handle_ReservedHolderAtFullLot_AllowsWithSpaceLabelAndReservedSession()
  {
    GivenLot(generalCapacity: 1, activeGeneralSessions: 5);
    var driver = GivenKnownDriver();
    var spaceId = ParkingSpaceId.From(Guid.NewGuid());
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns(Reservation.Create(spaceId, driver.Id, _lotId, actorId: null));
    _gateLotReader.FindActiveSpaceLabelAsync(_lotId, spaceId, Arg.Any<CancellationToken>()).Returns("R1");

    ParkingSession? opened = null;
    await _sessionRepository.AddAsync(Arg.Do<ParkingSession>(s => opened = s), Arg.Any<CancellationToken>());

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    result.Value.Pool.ShouldBe(SessionPool.Reserved);
    result.Value.ReservedSpaceLabel.ShouldBe("R1");

    opened.ShouldNotBeNull();
    opened.SpaceId.ShouldBe(spaceId);
    opened.DriverId.ShouldBe(driver.Id);
  }

  [Fact]
  public async Task Handle_ReservationOnAnInactiveSpace_IsTreatedAsNoReservation()
  {
    GivenLot(generalCapacity: 1, activeGeneralSessions: 1);
    var driver = GivenKnownDriver();
    var spaceId = ParkingSpaceId.From(Guid.NewGuid());
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns(Reservation.Create(spaceId, driver.Id, _lotId, actorId: null));
    _gateLotReader.FindActiveSpaceLabelAsync(_lotId, spaceId, Arg.Any<CancellationToken>()).Returns((string?)null);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotFull);
  }

  [Fact]
  public async Task Handle_EnterDeniedByFullBlockingLot_OpensNoSession()
  {
    GivenLot(generalCapacity: 1, activeGeneralSessions: 1);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotFull);
    result.Value.SessionId.ShouldBeNull();
    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_EnterUnknownPlateOnRestrictedLot_DeniesNotAuthorized()
  {
    GivenLot(accessMode: AccessMode.Restricted);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.NotAuthorized);
  }

  [Fact]
  public async Task Handle_EnterWithGrantExpiredBeforeTheEvent_DeniesOnRestrictedLot()
  {
    GivenLot(accessMode: AccessMode.Restricted);
    var driver = GivenKnownDriver();
    var expired = AccessGrant.Create(driver.Id, _lotId, Now.AddDays(-10), Now.AddDays(-1), actorId: null);
    _grantRepository.ListAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns([expired]);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.NotAuthorized);
  }

  [Fact]
  public async Task Handle_EnterWithGrantValidAtTheEvent_AllowsOnRestrictedLot()
  {
    GivenLot(accessMode: AccessMode.Restricted);
    var driver = GivenKnownDriver();
    var grant = AccessGrant.Create(driver.Id, _lotId, Now.AddDays(-1), Now.AddDays(1), actorId: null);
    _grantRepository.ListAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns([grant]);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    result.Value.Pool.ShouldBe(SessionPool.General);
  }

  [Fact]
  public async Task Handle_EnterWithDeactivatedPlateOnRestrictedLot_TreatsPlateAsUnknown()
  {
    GivenLot(accessMode: AccessMode.Restricted);
    var driver = Driver.Create("Jane Driver", null, actorId: null);
    var plate = driver.AddPlate("AAA 111", actorId: null);
    driver.DeactivatePlate(plate.Id, actorId: null);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<PlateByNormalizedValueSpec>(), Arg.Any<CancellationToken>())
      .Returns(driver);
    _grantRepository.ListAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns([AccessGrant.Create(driver.Id, _lotId, Now.AddDays(-1), null, actorId: null)]);

    var result = await CreateSut().Handle(Command(), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.NotAuthorized);
  }

  [Fact]
  public async Task Handle_Exit_ClosesTheMatchedSession()
  {
    GivenLot();
    var session = ParkingSession.OpenForEntry(_lotId, null, "AAA111", null, SessionPool.General,
      AccessEventId.From(Guid.NewGuid()), Now.AddHours(-2));
    _sessionRepository.FirstOrDefaultAsync(Arg.Any<MostRecentActiveSessionByPlateSpec>(), Arg.Any<CancellationToken>())
      .Returns(session);

    var result = await CreateSut().Handle(Command(direction: Direction.Exit), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    result.Value.SessionId.ShouldBe(session.Id.Value);
    session.Status.ShouldBe(SessionStatus.Closed);
    session.ExitTime.ShouldBe(Now);
    session.ExitEventId!.Value.Value.ShouldBe(result.Value.EventId!.Value);

    await _sessionRepository.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ExitWithNoOpenSession_RecordsAnomalyAndTouchesNoSession()
  {
    GivenLot();

    var result = await CreateSut().Handle(Command(direction: Direction.Exit), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.NoOpenSession);
    result.Value.SessionId.ShouldBeNull();
    await _accessEventRepository.Received(1).AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());
    await _sessionRepository.DidNotReceive().UpdateAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_PlateIsNormalizedBeforeMatching()
  {
    GivenLot();

    ParkingSession? opened = null;
    await _sessionRepository.AddAsync(Arg.Do<ParkingSession>(s => opened = s), Arg.Any<CancellationToken>());

    await CreateSut().Handle(Command(plate: " aaa 111 "), CancellationToken.None);

    opened.ShouldNotBeNull();
    opened.Plate.ShouldBe("AAA111");
  }

  [Fact]
  public async Task Handle_ManualEnter_TagsTheEventWithSourceAndActingStaff()
  {
    GivenLot();
    var staff = AccessEventActor.Staff(Guid.NewGuid());

    AccessEvent? recorded = null;
    await _accessEventRepository.AddAsync(Arg.Do<AccessEvent>(e => recorded = e), Arg.Any<CancellationToken>());

    var result = await CreateSut().Handle(Command(actor: staff, source: EventSource.Manual), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    recorded.ShouldNotBeNull();
    recorded.Source.ShouldBe(EventSource.Manual);
    recorded.ActingStaffId.ShouldBe(staff.Id);
    recorded.ActorId.ShouldBe(staff.Id);
    await _sessionRepository.Received(1).AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }
}
