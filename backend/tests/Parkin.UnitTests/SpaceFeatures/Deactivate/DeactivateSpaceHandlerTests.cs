using Ardalis.Result;
using Ardalis.SharedKernel;
using NSubstitute;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.SpaceFeatures;
using Parkin.Api.SpaceFeatures.Deactivate;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.SpaceFeatures.Deactivate;

public class DeactivateSpaceHandlerTests
{
  private readonly IRepository<ParkingLot> _repository = Substitute.For<IRepository<ParkingLot>>();
  private readonly IActiveReservationChecker _checker = Substitute.For<IActiveReservationChecker>();

  private DeactivateSpaceHandler CreateSut() => new(_repository, _checker);

  [Fact]
  public async Task Handle_ReservedSpaceWithActiveReservation_ReturnsInvalid()
  {
    var lot = ParkingLot.Create("Test Lot", "America/New_York");
    var space = lot.AddSpace("A1", SpaceType.Reserved, actorId: null);
    _repository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);
    _checker.HasActiveReservationAsync(space.Id, Arg.Any<CancellationToken>()).Returns(true);

    var result = await CreateSut().Handle(new DeactivateSpaceCommand(space.Id, ActorId: null), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Invalid);
    space.Status.ShouldBe(SpaceStatus.Active);
  }

  [Fact]
  public async Task Handle_ReservedSpaceWithoutActiveReservation_Deactivates()
  {
    var lot = ParkingLot.Create("Test Lot", "America/New_York");
    var space = lot.AddSpace("A1", SpaceType.Reserved, actorId: null);
    _repository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);
    _checker.HasActiveReservationAsync(space.Id, Arg.Any<CancellationToken>()).Returns(false);

    var result = await CreateSut().Handle(new DeactivateSpaceCommand(space.Id, ActorId: null), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    space.Status.ShouldBe(SpaceStatus.Inactive);
  }

  [Fact]
  public async Task Handle_GeneralSpace_DeactivatesWithoutCheckingReservations()
  {
    var lot = ParkingLot.Create("Test Lot", "America/New_York");
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);
    _repository.FirstOrDefaultAsync(Arg.Any<ParkingLotBySpaceIdSpec>(), Arg.Any<CancellationToken>())
      .Returns(lot);

    var result = await CreateSut().Handle(new DeactivateSpaceCommand(space.Id, ActorId: null), CancellationToken.None);

    result.Status.ShouldBe(ResultStatus.Ok);
    space.Status.ShouldBe(SpaceStatus.Inactive);
    _checker.ReceivedCalls().ShouldBeEmpty();
  }
}
