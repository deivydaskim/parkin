using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.Exceptions;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.Features.Reservations.Create;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.ReservationFeatures.Create;

public class CreateReservationHandlerTests
{
  private readonly IRepository<ParkingLot> _lotRepository = Substitute.For<IRepository<ParkingLot>>();
  private readonly IReadRepository<Driver> _driverRepository = Substitute.For<IReadRepository<Driver>>();
  private readonly IRepository<Reservation> _reservationRepository = Substitute.For<IRepository<Reservation>>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

  public CreateReservationHandlerTests()
  {
    _unitOfWork
      .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
      .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
  }

  private CreateReservationHandler CreateSut() =>
    new(_lotRepository, _driverRepository, _reservationRepository, _unitOfWork);

  private static ParkingLot CreateLotWithSpace(out ParkingSpace space, SpaceType type = SpaceType.Reserved)
  {
    var lot = ParkingLot.Create("Test Lot", "America/New_York");
    space = lot.AddSpace("A1", type, actorId: null).Value;
    return lot;
  }

  private void GivenLot(ParkingLot? lot) =>
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);

  private void GivenDriverExists(bool exists) =>
    _driverRepository.AnyAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>()).Returns(exists);

  private void GivenSpaceReserved(bool reserved) =>
    _reservationRepository.AnyAsync(Arg.Any<ActiveReservationBySpaceSpec>(), Arg.Any<CancellationToken>())
      .Returns(reserved);

  private void GivenDriverReservedInLot(bool reserved) =>
    _reservationRepository.AnyAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns(reserved);

  [Fact]
  public async Task Handle_SpaceNotFound_ReturnsNotFound()
  {
    GivenLot(null);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(ParkingSpaceId.From(Guid.NewGuid()), DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_SpaceInactive_ReturnsInvalid()
  {
    var lot = CreateLotWithSpace(out var space);
    lot.DeactivateSpace(space.Id, actorId: null);
    GivenLot(lot);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_DriverNotFound_ReturnsNotFound()
  {
    GivenLot(CreateLotWithSpace(out var space));
    GivenDriverExists(false);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_SpaceAlreadyHasActiveReservation_ReturnsConflictWithoutTouchingSpace()
  {
    GivenLot(CreateLotWithSpace(out var space, SpaceType.General));
    GivenDriverExists(true);
    GivenSpaceReserved(true);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    space.Type.ShouldBe(SpaceType.General);
  }

  [Fact]
  public async Task Handle_DriverAlreadyHasActiveReservationInLot_ReturnsConflict()
  {
    GivenLot(CreateLotWithSpace(out var space));
    GivenDriverExists(true);
    GivenSpaceReserved(false);
    GivenDriverReservedInLot(true);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
  }

  [Fact]
  public async Task Handle_HappyPath_CreatesReservationAndFlipsSpaceToReservedInOneTransaction()
  {
    var lot = CreateLotWithSpace(out var space, SpaceType.General);
    var driverId = DriverId.From(Guid.NewGuid());
    GivenLot(lot);
    GivenDriverExists(true);
    GivenSpaceReserved(false);
    GivenDriverReservedInLot(false);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, driverId, null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.SpaceId.ShouldBe(space.Id.Value);
    result.Value.DriverId.ShouldBe(driverId.Value);
    result.Value.LotId.ShouldBe(lot.Id.Value);
    result.Value.Status.ShouldBe(ReservationStatus.Active);
    space.Type.ShouldBe(SpaceType.Reserved);
    await _unitOfWork.Received(1).ExecuteInTransactionAsync(
      Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    await _reservationRepository.Received(1).AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>());
    await _lotRepository.Received(1).UpdateAsync(lot, Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(Reservation.ActiveSpaceIndex, "Space already has an active reservation")]
  [InlineData(Reservation.ActiveDriverLotIndex, "Driver already has an active reservation in this lot")]
  public async Task Handle_LostRaceOnActiveReservationIndex_ReturnsConflictWithMessage(string constraint,
    string expectedMessage)
  {
    GivenLot(CreateLotWithSpace(out var space));
    GivenDriverExists(true);
    GivenSpaceReserved(false);
    GivenDriverReservedInLot(false);
    _reservationRepository.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>())
      .ThrowsAsync(new UniqueConstraintViolationException(constraint, new InvalidOperationException()));

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    result.Errors.ShouldHaveSingleItem().ShouldBe(expectedMessage);
  }

  [Fact]
  public async Task Handle_UnrelatedUniqueViolation_Propagates()
  {
    GivenLot(CreateLotWithSpace(out var space));
    GivenDriverExists(true);
    GivenSpaceReserved(false);
    GivenDriverReservedInLot(false);
    _reservationRepository.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>())
      .ThrowsAsync(new UniqueConstraintViolationException("some_other_index", new InvalidOperationException()));

    await Should.ThrowAsync<UniqueConstraintViolationException>(async () => await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None));
  }
}
