using Ardalis.Result;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.ParkingLotAggregate;

public class ParkingLotSpaceTests
{
  private static readonly ParkingSpaceId UnknownSpaceId = ParkingSpaceId.From(Guid.NewGuid());

  private static ParkingLot CreateLot() => ParkingLot.Create("Test Lot", "America/New_York");

  private static ParkingSpace AddSpace(ParkingLot lot, string label, SpaceType type = SpaceType.General) =>
    lot.AddSpace(label, type, actorId: null).Value;

  [Fact]
  public void AddSpace_AddsSpaceToLot()
  {
    var lot = CreateLot();

    var result = lot.AddSpace("A1", SpaceType.General, actorId: null);

    result.IsSuccess.ShouldBeTrue();
    lot.Spaces.ShouldContain(result.Value);
    result.Value.Label.ShouldBe("A1");
    result.Value.Type.ShouldBe(SpaceType.General);
    result.Value.Status.ShouldBe(SpaceStatus.Active);
  }

  [Fact]
  public void AddSpace_DuplicateLabel_ReturnsInvalid()
  {
    var lot = CreateLot();
    AddSpace(lot, "A1");

    var result = lot.AddSpace("A1", SpaceType.Reserved, actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    result.ValidationErrors.ShouldContain(e => e.Identifier == "Label");
    lot.Spaces.Count.ShouldBe(1);
  }

  [Fact]
  public void AddSpace_BlankLabel_ReturnsInvalid()
  {
    var lot = CreateLot();

    lot.AddSpace("  ", SpaceType.General, actorId: null).Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void Capacity_CountsOnlyActiveGeneralSpaces()
  {
    var lot = CreateLot();
    var general = AddSpace(lot, "A1");
    AddSpace(lot, "A2", SpaceType.Reserved);
    var inactiveGeneral = AddSpace(lot, "A3");
    lot.DeactivateSpace(inactiveGeneral.Id, actorId: null);

    lot.Capacity.ShouldBe(1);
    general.Status.ShouldBe(SpaceStatus.Active);
  }

  [Fact]
  public void DeactivateSpace_FlipsStatusAndRegistersEvent()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");
    var actorId = Guid.NewGuid();

    var result = lot.DeactivateSpace(space.Id, actorId);

    result.Value.ShouldBeSameAs(space);
    space.Status.ShouldBe(SpaceStatus.Inactive);
    lot.DomainEvents.OfType<SpaceDeactivatedEvent>()
      .ShouldContain(e => e.SpaceId == space.Id && e.LotId == lot.Id && e.ActorId == actorId);
  }

  [Fact]
  public void DeactivateSpace_UnknownSpace_ReturnsNotFoundWithoutEvent()
  {
    var lot = CreateLot();
    lot.ClearDomainEvents();

    lot.DeactivateSpace(UnknownSpaceId, actorId: null).Status.ShouldBe(ResultStatus.NotFound);

    lot.DomainEvents.ShouldBeEmpty();
  }

  [Fact]
  public void ReactivateSpace_RestoresStatusAndRegistersEvent()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");
    lot.DeactivateSpace(space.Id, actorId: null);

    var result = lot.ReactivateSpace(space.Id, actorId: null);

    result.IsSuccess.ShouldBeTrue();
    space.Status.ShouldBe(SpaceStatus.Active);
    lot.DomainEvents.OfType<SpaceReactivatedEvent>().ShouldContain(e => e.SpaceId == space.Id);
  }

  [Fact]
  public void ReactivateSpace_UnknownSpace_ReturnsNotFound()
  {
    CreateLot().ReactivateSpace(UnknownSpaceId, actorId: null).Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public void UpdateSpace_RenamesAndRetypes()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");

    var result = lot.UpdateSpace(space.Id, "A1-renamed", SpaceType.Reserved, actorId: null);

    result.IsSuccess.ShouldBeTrue();
    space.Label.ShouldBe("A1-renamed");
    space.Type.ShouldBe(SpaceType.Reserved);
  }

  [Fact]
  public void UpdateSpace_LabelTakenByAnotherSpace_ReturnsInvalidAndKeepsLabel()
  {
    var lot = CreateLot();
    AddSpace(lot, "A1");
    var space = AddSpace(lot, "A2");

    var result = lot.UpdateSpace(space.Id, new SpaceUpdate(Label: "A1"), actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    space.Label.ShouldBe("A2");
  }

  [Fact]
  public void UpdateSpace_KeepingItsOwnLabel_Succeeds()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");

    lot.UpdateSpace(space.Id, new SpaceUpdate(Label: "A1"), actorId: null).IsSuccess.ShouldBeTrue();
  }

  [Fact]
  public void UpdateSpace_UnknownSpace_ReturnsNotFound()
  {
    CreateLot().UpdateSpace(UnknownSpaceId, new SpaceUpdate(Label: "X"), actorId: null)
      .Status.ShouldBe(ResultStatus.NotFound);
  }
}
