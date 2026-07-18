using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.ParkingLotAggregate;

public class ParkingLotSpaceTests
{
  private static ParkingLot CreateLot() => ParkingLot.Create("Test Lot", "America/New_York");

  [Fact]
  public void AddSpace_AddsSpaceToLot()
  {
    var lot = CreateLot();

    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);

    lot.Spaces.ShouldContain(space);
    space.Label.ShouldBe("A1");
    space.Type.ShouldBe(SpaceType.General);
    space.Status.ShouldBe(SpaceStatus.Active);
  }

  [Fact]
  public void Capacity_CountsOnlyActiveGeneralSpaces()
  {
    var lot = CreateLot();
    var general = lot.AddSpace("A1", SpaceType.General, actorId: null);
    lot.AddSpace("A2", SpaceType.Reserved, actorId: null);
    var inactiveGeneral = lot.AddSpace("A3", SpaceType.General, actorId: null);
    lot.DeactivateSpace(inactiveGeneral.Id, actorId: null);

    lot.Capacity.ShouldBe(1);
    general.Status.ShouldBe(SpaceStatus.Active);
  }

  [Fact]
  public void DeactivateSpace_FlipsStatusAndRegistersEvent()
  {
    var lot = CreateLot();
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);
    var actorId = Guid.NewGuid();

    lot.DeactivateSpace(space.Id, actorId);

    space.Status.ShouldBe(SpaceStatus.Inactive);
    lot.DomainEvents.ShouldContain(e => e is SpaceDeactivatedEvent
      && ((SpaceDeactivatedEvent)e).SpaceId == space.Id
      && ((SpaceDeactivatedEvent)e).LotId == lot.Id
      && ((SpaceDeactivatedEvent)e).ActorId == actorId);
  }

  [Fact]
  public void UpdateSpace_RenamesAndRetypes()
  {
    var lot = CreateLot();
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);

    lot.UpdateSpace(space.Id, "A1-renamed", SpaceType.Reserved, actorId: null);

    space.Label.ShouldBe("A1-renamed");
    space.Type.ShouldBe(SpaceType.Reserved);
  }
}
