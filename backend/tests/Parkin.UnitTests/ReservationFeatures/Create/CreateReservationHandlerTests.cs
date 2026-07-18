using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Domain.ReservationAggregate.Specifications;
using Parkin.Api.ReservationFeatures.Create;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.ReservationFeatures.Create;

public class CreateReservationHandlerTests
{
  private readonly IRepository<ParkingLot> _lotRepository = Substitute.For<IRepository<ParkingLot>>();
  private readonly IRepository<Driver> _driverRepository = Substitute.For<IRepository<Driver>>();
  private readonly IRepository<Reservation> _reservationRepository = Substitute.For<IRepository<Reservation>>();

  private CreateReservationHandler CreateSut() => new(_lotRepository, _driverRepository, _reservationRepository);

  private static ParkingLot CreateLotWithSpace(out ParkingSpace space, SpaceType type = SpaceType.Reserved)
  {
    var lot = ParkingLot.Create("Test Lot", "America/New_York");
    space = lot.AddSpace("A1", type, actorId: null);
    return lot;
  }

  private static Driver CreateDriver() => Driver.Create("Jane Driver", null, actorId: null);

  [Fact]
  public async Task Handle_SpaceNotFound_ReturnsNotFound()
  {
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((ParkingLot?)null);

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
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_DriverNotFound_ReturnsNotFound()
  {
    var lot = CreateLotWithSpace(out var space);
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((Driver?)null);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, DriverId.From(Guid.NewGuid()), null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_SpaceAlreadyHasActiveReservation_ReturnsConflict()
  {
    var lot = CreateLotWithSpace(out var space);
    var driver = CreateDriver();
    var existing = Reservation.Create(space.Id, DriverId.From(Guid.NewGuid()), lot.Id, null);
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(driver);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationBySpaceSpec>(), Arg.Any<CancellationToken>())
      .Returns(existing);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, driver.Id, null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
    space.Type.ShouldBe(SpaceType.Reserved);
  }

  [Fact]
  public async Task Handle_DriverAlreadyHasActiveReservationInLot_ReturnsConflict()
  {
    var lot = CreateLotWithSpace(out var space);
    var driver = CreateDriver();
    var existing = Reservation.Create(ParkingSpaceId.From(Guid.NewGuid()), driver.Id, lot.Id, null);
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(driver);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationBySpaceSpec>(), Arg.Any<CancellationToken>())
      .Returns((Reservation?)null);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns(existing);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, driver.Id, null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Conflict);
  }

  [Fact]
  public async Task Handle_HappyPath_CreatesReservationAndFlipsSpaceToReserved()
  {
    var lot = CreateLotWithSpace(out var space, SpaceType.General);
    var driver = CreateDriver();
    _lotRepository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);
    _driverRepository.FirstOrDefaultAsync(Arg.Any<DriverByIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(driver);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationBySpaceSpec>(), Arg.Any<CancellationToken>())
      .Returns((Reservation?)null);
    _reservationRepository.FirstOrDefaultAsync(Arg.Any<ActiveReservationByDriverLotSpec>(), Arg.Any<CancellationToken>())
      .Returns((Reservation?)null);

    var result = await CreateSut().Handle(
      new CreateReservationCommand(space.Id, driver.Id, null),
      CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    result.Value.SpaceId.ShouldBe(space.Id);
    result.Value.DriverId.ShouldBe(driver.Id);
    result.Value.LotId.ShouldBe(lot.Id);
    result.Value.Status.ShouldBe(ReservationStatus.Active);
    space.Type.ShouldBe(SpaceType.Reserved);
    await _reservationRepository.Received(1).AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>());
    await _lotRepository.Received(1).UpdateAsync(lot, Arg.Any<CancellationToken>());
  }
}
