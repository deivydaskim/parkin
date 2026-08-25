using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.AccessEventFeatures.Ingest;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.AccessEventAggregate.Specifications;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate.Specifications;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.Domain.Services;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.AccessEventFeatures.Ingest;

public class IngestAccessEventHandlerTests
{
  private readonly IRepository<AccessEvent> _accessEventRepository = Substitute.For<IRepository<AccessEvent>>();
  private readonly IRepository<ParkingSession> _sessionRepository = Substitute.For<IRepository<ParkingSession>>();
  private readonly IRepository<AuditLogEntry> _auditRepository = Substitute.For<IRepository<AuditLogEntry>>();
  private readonly IReadRepository<ParkingLot> _lotRepository = Substitute.For<IReadRepository<ParkingLot>>();
  private readonly IReadRepository<Driver> _driverRepository = Substitute.For<IReadRepository<Driver>>();
  private readonly IReadRepository<AccessGrant> _grantRepository = Substitute.For<IReadRepository<AccessGrant>>();
  private readonly IReadRepository<Reservation> _reservationRepository = Substitute.For<IReadRepository<Reservation>>();
  private readonly ILotRowLocker _lotRowLocker = Substitute.For<ILotRowLocker>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

  private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

  public IngestAccessEventHandlerTests()
  {
    // Run the action inline - the lock and the idempotency index are proven against a real Postgres
    // in Parkin.IntegrationTests, not here.
    _unitOfWork
      .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
      .Returns(callInfo => ((Func<CancellationToken, Task>)callInfo[0])(CancellationToken.None));

    _accessEventRepository
      .FirstOrDefaultAsync(Arg.Any<AccessEventByIdempotencyKeySpec>(), Arg.Any<CancellationToken>())
      .Returns((AccessEvent?)null);
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
    _sessionRepository
      .CountAsync(Arg.Any<ActiveSessionCountByLotPoolSpec>(), Arg.Any<CancellationToken>())
      .Returns(0);
  }

  private IngestAccessEventHandler CreateSut() => new(
    _accessEventRepository, _sessionRepository, _auditRepository, _lotRepository, _driverRepository,
    _grantRepository, _reservationRepository, new EntryDecisionService(), new OccupancyCalculator(),
    _lotRowLocker, _unitOfWork);

  private static IngestAccessEventCommand Command(ParkingLotId lotId, string plate = "AAA 111",
    Direction direction = Direction.Enter, string idempotencyKey = "key-1") =>
    new(lotId, plate, direction, EventSource.Lpr, Now, idempotencyKey, ActorId: Guid.NewGuid());

  private ParkingLot GivenLot(AccessMode accessMode = AccessMode.Open,
    FullBehavior fullBehavior = FullBehavior.Block, int generalSpaces = 5)
  {
    var lot = ParkingLot.Create("Main", "Europe/Vilnius", accessMode: accessMode, fullBehavior: fullBehavior);
    for (var i = 0; i < generalSpaces; i++)
    {
      lot.AddSpace($"G{i}", SpaceType.General, actorId: null);
    }

    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotByIdSpec>(), Arg.Any<CancellationToken>()).Returns(lot);
    return lot;
  }

  private Driver GivenKnownDriver(string plate = "AAA 111")
  {
    var driver = Driver.Create("Jane Driver", null, actorId: null);
    driver.AddPlate(plate, actorId: null);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<PlateByNormalizedValueSpec>(), Arg.Any<CancellationToken>())
      .Returns(driver);
    return driver;
  }

  [Fact]
  public async Task Handle_ReplayedIdempotencyKey_ReturnsOriginalAndWritesNothing()
  {
    var lot = GivenLot();
    var original = AccessEvent.Record(lot.Id, "AAA 111", "AAA111", null, null, Direction.Enter,
      EventSource.Lpr, Decision.Deny, DenyReason.LotFull, Now, "key-1", actorId: null);
    _accessEventRepository
      .FirstOrDefaultAsync(Arg.Any<AccessEventByIdempotencyKeySpec>(), Arg.Any<CancellationToken>())
      .Returns(original);

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.EventId.ShouldBe(original.Id);
    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotFull);

    await _accessEventRepository.DidNotReceive().AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());
    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
      Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_UnknownLot_DeniesWithoutPersistingAnEvent()
  {
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((ParkingLot?)null);

    var result = await CreateSut().Handle(Command(ParkingLotId.From(Guid.NewGuid())), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotNotFound);
    result.Value.EventId.ShouldBeNull();

    await _accessEventRepository.DidNotReceive().AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());
    await _auditRepository.Received(1).AddAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ArchivedLot_DeniesAndPersistsTheEvent()
  {
    var lot = GivenLot();
    lot.Archive(actorId: null);

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotArchived);
    result.Value.EventId.ShouldNotBeNull();

    await _accessEventRepository.Received(1).AddAsync(Arg.Any<AccessEvent>(), Arg.Any<CancellationToken>());
    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_EnterAllowedOnOpenLot_OpensGeneralSessionWithNoSpace()
  {
    var lot = GivenLot();

    ParkingSession? opened = null;
    await _sessionRepository.AddAsync(Arg.Do<ParkingSession>(s => opened = s), Arg.Any<CancellationToken>());

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

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

    await _lotRowLocker.Received(1).LockAsync(lot.Id, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReservedHolderAtFullLot_AllowsWithSpaceLabelAndReservedSession()
  {
    var lot = GivenLot(generalSpaces: 1);
    var reservedSpace = lot.AddSpace("R1", SpaceType.Reserved, actorId: null);
    var driver = GivenKnownDriver();

    _sessionRepository.CountAsync(Arg.Any<ActiveSessionCountByLotPoolSpec>(), Arg.Any<CancellationToken>())
      .Returns(5); // general pool well past capacity
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns(Reservation.Create(reservedSpace.Id, driver.Id, lot.Id, actorId: null));

    ParkingSession? opened = null;
    await _sessionRepository.AddAsync(Arg.Do<ParkingSession>(s => opened = s), Arg.Any<CancellationToken>());

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    result.Value.Pool.ShouldBe(SessionPool.Reserved);
    result.Value.ReservedSpaceLabel.ShouldBe("R1");

    opened.ShouldNotBeNull();
    opened.SpaceId.ShouldBe(reservedSpace.Id);
    opened.DriverId.ShouldBe(driver.Id);
  }

  [Fact]
  public async Task Handle_EnterDeniedByFullBlockingLot_OpensNoSession()
  {
    var lot = GivenLot(generalSpaces: 1);
    _sessionRepository.CountAsync(Arg.Any<ActiveSessionCountByLotPoolSpec>(), Arg.Any<CancellationToken>())
      .Returns(1);

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.LotFull);
    result.Value.SessionId.ShouldBeNull();

    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_EnterUnknownPlateOnRestrictedLot_DeniesNotAuthorized()
  {
    var lot = GivenLot(accessMode: AccessMode.Restricted);

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.NotAuthorized);
  }

  [Fact]
  public async Task Handle_EnterWithGrantExpiredBeforeTheEvent_DeniesOnRestrictedLot()
  {
    var lot = GivenLot(accessMode: AccessMode.Restricted);
    var driver = GivenKnownDriver();
    var expired = AccessGrant.Create(driver.Id, lot.Id, Now.AddDays(-10), Now.AddDays(-1), actorId: null);
    _grantRepository.ListAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns([expired]);

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.NotAuthorized);
  }

  [Fact]
  public async Task Handle_EnterWithGrantValidAtTheEvent_AllowsOnRestrictedLot()
  {
    var lot = GivenLot(accessMode: AccessMode.Restricted);
    var driver = GivenKnownDriver();
    var grant = AccessGrant.Create(driver.Id, lot.Id, Now.AddDays(-1), Now.AddDays(1), actorId: null);
    _grantRepository.ListAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns([grant]);

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    result.Value.Pool.ShouldBe(SessionPool.General);
  }

  [Fact]
  public async Task Handle_EnterWithDeactivatedPlateOnRestrictedLot_TreatsPlateAsUnknown()
  {
    var lot = GivenLot(accessMode: AccessMode.Restricted);
    var driver = Driver.Create("Jane Driver", null, actorId: null);
    var plate = driver.AddPlate("AAA 111", actorId: null);
    driver.DeactivatePlate(plate.Id, actorId: null);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<PlateByNormalizedValueSpec>(), Arg.Any<CancellationToken>())
      .Returns(driver);
    _grantRepository.ListAsync(Arg.Any<ActiveGrantForDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns([AccessGrant.Create(driver.Id, lot.Id, Now.AddDays(-1), null, actorId: null)]);

    var result = await CreateSut().Handle(Command(lot.Id), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Deny);
    result.Value.Reason.ShouldBe(DenyReason.NotAuthorized);
  }

  [Fact]
  public async Task Handle_Exit_ClosesTheMatchedSession()
  {
    var lot = GivenLot();
    var session = ParkingSession.OpenForEntry(lot.Id, null, "AAA111", null, SessionPool.General,
      AccessEventId.From(Guid.NewGuid()), Now.AddHours(-2));
    _sessionRepository.FirstOrDefaultAsync(Arg.Any<MostRecentActiveSessionByPlateSpec>(), Arg.Any<CancellationToken>())
      .Returns(session);

    var result = await CreateSut().Handle(
      Command(lot.Id, direction: Direction.Exit), CancellationToken.None);

    result.Value.Decision.ShouldBe(Decision.Allow);
    result.Value.SessionId.ShouldBe(session.Id);

    session.Status.ShouldBe(SessionStatus.Closed);
    session.ExitTime.ShouldBe(Now);
    session.ExitEventId.ShouldBe(result.Value.EventId);

    await _sessionRepository.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
    await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ExitWithNoOpenSession_RecordsAnomalyAndTouchesNoSession()
  {
    var lot = GivenLot();

    var result = await CreateSut().Handle(
      Command(lot.Id, direction: Direction.Exit), CancellationToken.None);

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
    var lot = GivenLot();

    ParkingSession? opened = null;
    await _sessionRepository.AddAsync(Arg.Do<ParkingSession>(s => opened = s), Arg.Any<CancellationToken>());

    await CreateSut().Handle(Command(lot.Id, plate: " aaa 111 "), CancellationToken.None);

    opened.ShouldNotBeNull();
    opened.Plate.ShouldBe("AAA111");
  }
}
