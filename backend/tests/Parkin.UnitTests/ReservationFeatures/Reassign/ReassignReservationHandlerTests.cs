using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Exceptions;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.Features.Reservations.Reassign;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.ReservationFeatures.Reassign;

public class ReassignReservationHandlerTests
{
  private static readonly ParkingSpaceId SpaceId = ParkingSpaceId.From(Guid.NewGuid());
  private static readonly ParkingLotId LotId = ParkingLotId.From(Guid.NewGuid());

  private readonly IRepository<Reservation> _reservationRepository = Substitute.For<IRepository<Reservation>>();
  private readonly IReadRepository<Driver> _driverRepository = Substitute.For<IReadRepository<Driver>>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

  public ReassignReservationHandlerTests()
  {
    _unitOfWork
      .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
      .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
  }

  private ReassignReservationHandler CreateSut() => new(_reservationRepository, _driverRepository, _unitOfWork);

  private Reservation GivenActiveReservation(DriverId? driverId = null)
  {
    var reservation = Reservation.Create(SpaceId, driverId ?? DriverId.From(Guid.NewGuid()), LotId, actorId: null);
    _reservationRepository.GetByIdAsync(reservation.Id, Arg.Any<CancellationToken>()).Returns(reservation);
    return reservation;
  }

  private void GivenDriverExists(bool exists) =>
    _driverRepository.AnyAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>()).Returns(exists);

  private void GivenNewDriverReservedInLot(bool reserved) =>
    _reservationRepository.AnyAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns(reserved);

  private static ReassignReservationCommand Command(Reservation reservation, DriverId? newDriverId = null) =>
    new(reservation.Id, newDriverId ?? DriverId.From(Guid.NewGuid()), null);

  [Fact]
  public async Task Handle_ReservationNotFound_ReturnsNotFound()
  {
    var result = await CreateSut().Handle(
      new ReassignReservationCommand(ReservationId.From(Guid.NewGuid()), DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_ReservationNotActive_ReturnsInvalid()
  {
    var oldReservation = GivenActiveReservation();
    oldReservation.Cancel(null);

    var result = await CreateSut().Handle(Command(oldReservation), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_SameDriver_ReturnsInvalid()
  {
    var driverId = DriverId.From(Guid.NewGuid());
    var oldReservation = GivenActiveReservation(driverId);

    var result = await CreateSut().Handle(Command(oldReservation, driverId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_NewDriverNotFound_ReturnsNotFoundWithoutSaving()
  {
    var oldReservation = GivenActiveReservation();
    GivenDriverExists(false);

    var result = await CreateSut().Handle(Command(oldReservation), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
    await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
      Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NewDriverAlreadyHasActiveReservationInLot_ReturnsConflict()
  {
    var oldReservation = GivenActiveReservation();
    GivenDriverExists(true);
    GivenNewDriverReservedInLot(true);

    var result = await CreateSut().Handle(Command(oldReservation), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
  }

  [Fact]
  public async Task Handle_LostRaceOnDriverLotIndex_ReturnsConflict()
  {
    var oldReservation = GivenActiveReservation();
    GivenDriverExists(true);
    GivenNewDriverReservedInLot(false);
    _reservationRepository.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>())
      .ThrowsAsync(new UniqueConstraintViolationException(Reservation.ActiveDriverLotIndex,
        new InvalidOperationException()));

    var result = await CreateSut().Handle(Command(oldReservation), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    result.Errors.ShouldHaveSingleItem().ShouldBe("Driver already has an active reservation in this lot");
  }

  [Fact]
  public async Task Handle_HappyPath_CancelsOldAndCreatesNewActiveReservation()
  {
    var oldReservation = GivenActiveReservation();
    var newDriverId = DriverId.From(Guid.NewGuid());
    GivenDriverExists(true);
    GivenNewDriverReservedInLot(false);

    var result = await CreateSut().Handle(Command(oldReservation, newDriverId), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.SpaceId.ShouldBe(SpaceId.Value);
    result.Value.DriverId.ShouldBe(newDriverId.Value);
    result.Value.LotId.ShouldBe(LotId.Value);
    result.Value.Status.ShouldBe(ReservationStatus.Active);
    result.Value.Id.ShouldNotBe(oldReservation.Id.Value);

    oldReservation.Status.ShouldBe(ReservationStatus.Cancelled);

    Received.InOrder(() =>
    {
      _reservationRepository.UpdateAsync(oldReservation, Arg.Any<CancellationToken>());
      _reservationRepository.AddAsync(Arg.Is<Reservation>(r => r.DriverId == newDriverId), Arg.Any<CancellationToken>());
    });
    await _unitOfWork.Received(1).ExecuteInTransactionAsync(
      Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
  }
}
