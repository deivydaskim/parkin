using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.ReservationFeatures.Reassign;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.ReservationFeatures.Reassign;

public class ReassignReservationHandlerTests
{
  private readonly IRepository<Reservation> _reservationRepository = Substitute.For<IRepository<Reservation>>();
  private readonly IRepository<Driver> _driverRepository = Substitute.For<IRepository<Driver>>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

  public ReassignReservationHandlerTests()
  {
    // Simulate the real EfUnitOfWork by just running the action inline — the atomicity
    // guarantee itself is proven separately by the Postgres-backed integration test.
    _unitOfWork
      .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
      .Returns(callInfo => ((Func<CancellationToken, Task>)callInfo[0])(CancellationToken.None));
  }

  private ReassignReservationHandler CreateSut() => new(_reservationRepository, _driverRepository, _unitOfWork);

  private static Reservation CreateActiveReservation(ParkingSpaceId spaceId, DriverId driverId, ParkingLotId lotId) =>
    Reservation.Create(spaceId, driverId, lotId, actorId: null);

  private static Driver CreateDriver() => Driver.Create("Jane Driver", null, actorId: null);

  [Fact]
  public async Task Handle_ReservationNotFound_ReturnsNotFound()
  {
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ReservationByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((Reservation?)null);

    var result = await CreateSut().Handle(
      new ReassignReservationCommand(ReservationId.From(Guid.NewGuid()), DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_ReservationNotActive_ReturnsInvalid()
  {
    var oldReservation = CreateActiveReservation(
      ParkingSpaceId.From(Guid.NewGuid()), DriverId.From(Guid.NewGuid()), ParkingLotId.From(Guid.NewGuid()));
    oldReservation.Cancel(null);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ReservationByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(oldReservation);

    var result = await CreateSut().Handle(
      new ReassignReservationCommand(oldReservation.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_SameDriver_ReturnsInvalid()
  {
    var driverId = DriverId.From(Guid.NewGuid());
    var oldReservation = CreateActiveReservation(
      ParkingSpaceId.From(Guid.NewGuid()), driverId, ParkingLotId.From(Guid.NewGuid()));
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ReservationByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(oldReservation);

    var result = await CreateSut().Handle(
      new ReassignReservationCommand(oldReservation.Id, driverId, null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_NewDriverNotFound_ReturnsNotFound()
  {
    var oldReservation = CreateActiveReservation(
      ParkingSpaceId.From(Guid.NewGuid()), DriverId.From(Guid.NewGuid()), ParkingLotId.From(Guid.NewGuid()));
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ReservationByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(oldReservation);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((Driver?)null);

    var result = await CreateSut().Handle(
      new ReassignReservationCommand(oldReservation.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_NewDriverAlreadyHasActiveReservationInLot_ReturnsConflict()
  {
    var lotId = ParkingLotId.From(Guid.NewGuid());
    var oldReservation = CreateActiveReservation(ParkingSpaceId.From(Guid.NewGuid()), DriverId.From(Guid.NewGuid()), lotId);
    var newDriver = CreateDriver();
    var conflicting = CreateActiveReservation(ParkingSpaceId.From(Guid.NewGuid()), newDriver.Id, lotId);

    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ReservationByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(oldReservation);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(newDriver);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns(conflicting);

    var result = await CreateSut().Handle(
      new ReassignReservationCommand(oldReservation.Id, newDriver.Id, null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
  }

  [Fact]
  public async Task Handle_HappyPath_CancelsOldAndCreatesNewActiveReservation()
  {
    var spaceId = ParkingSpaceId.From(Guid.NewGuid());
    var lotId = ParkingLotId.From(Guid.NewGuid());
    var oldReservation = CreateActiveReservation(spaceId, DriverId.From(Guid.NewGuid()), lotId);
    var newDriver = CreateDriver();
    var actorId = Guid.NewGuid();

    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ReservationByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(oldReservation);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(newDriver);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns((Reservation?)null);

    var result = await CreateSut().Handle(
      new ReassignReservationCommand(oldReservation.Id, newDriver.Id, actorId),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.SpaceId.ShouldBe(spaceId);
    result.Value.DriverId.ShouldBe(newDriver.Id);
    result.Value.LotId.ShouldBe(lotId);
    result.Value.Status.ShouldBe(ReservationStatus.Active);
    result.Value.Id.ShouldNotBe(oldReservation.Id);

    oldReservation.Status.ShouldBe(ReservationStatus.Cancelled);

    await _reservationRepository.Received(1).UpdateAsync(oldReservation, Arg.Any<CancellationToken>());
    await _reservationRepository.Received(1).AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).ExecuteInTransactionAsync(
      Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
  }
}
