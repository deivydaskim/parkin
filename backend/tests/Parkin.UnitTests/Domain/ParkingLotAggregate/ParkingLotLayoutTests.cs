using Ardalis.Result;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.ParkingLotAggregate;

public class ParkingLotLayoutTests
{
  private static ParkingLot CreateLot(LotLayout? layout = null) =>
    ParkingLot.Create("Layout Lot", "Europe/Vilnius", layout: layout);

  private static LotLayout Layout(decimal width = 40m, decimal length = 30m, int levels = 1) =>
    LotLayout.Create(width, length, levels).Value;

  private static SpacePlacement Placement(decimal x, decimal y, decimal rotation = 0m, int level = 0) =>
    SpacePlacement.Create(x, y, rotation, level).Value;

  private static ParkingSpace AddSpace(ParkingLot lot, string label, string? zone = null,
    SpacePlacement? placement = null) =>
    lot.AddSpace(label, SpaceType.General, actorId: null, zone, placement).Value;

  private static Result<ParkingSpace> Place(ParkingLot lot, ParkingSpace space, SpacePlacement? placement,
    string? zone = null, Guid? actorId = null) =>
    lot.UpdateSpace(space.Id, new SpaceUpdate(Zone: zone, Placement: placement, ClearPlacement: placement is null),
      actorId);

  private static Result UpdateLayout(ParkingLot lot, LotLayout? layout) =>
    lot.UpdateDetails(lot.Name, lot.Address, lot.Timezone, layout, actorId: null);

  [Fact]
  public void SpacePlacement_Create_AppliesDefaultBaySize()
  {
    var placement = SpacePlacement.Create(10m, 5m).Value;

    placement.Width.ShouldBe(SpacePlacement.DefaultWidth);
    placement.Length.ShouldBe(SpacePlacement.DefaultLength);
    placement.Level.ShouldBe(0);
    placement.RotationDegrees.ShouldBe(0m);
  }

  [Theory]
  [InlineData(-1, 0, 0, 0, 2.5, 5)]
  [InlineData(0, -0.5, 0, 0, 2.5, 5)]
  [InlineData(0, 0, 360, 0, 2.5, 5)]
  [InlineData(0, 0, -15, 0, 2.5, 5)]
  [InlineData(0, 0, 0, -1, 2.5, 5)]
  [InlineData(0, 0, 0, 0, 0, 5)]
  [InlineData(0, 0, 0, 0, 2.5, -5)]
  public void SpacePlacement_Create_RejectsOutOfRangeValues(
    double x, double y, double rotation, int level, double width, double length)
  {
    var result = SpacePlacement.Create((decimal)x, (decimal)y, (decimal)rotation, level, (decimal)width, (decimal)length);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void LotLayout_Create_RejectsZeroLevels()
  {
    LotLayout.Create(10m, 10m, 0).Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void PlaceSpace_WithoutLayout_AcceptsAnyValidPlacement()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");

    var result = Place(lot, space, Placement(500m, 250m, 90m, level: 3), "North");

    result.IsSuccess.ShouldBeTrue();
    space.Placement.ShouldBe(Placement(500m, 250m, 90m, level: 3));
    space.Zone.ShouldBe("North");
  }

  [Fact]
  public void PlaceSpace_RegistersSpaceUpdatedEvent()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");
    var actorId = Guid.NewGuid();

    Place(lot, space, Placement(5m, 5m), actorId: actorId);

    lot.DomainEvents.OfType<SpaceUpdatedEvent>()
      .ShouldContain(e => e.SpaceId == space.Id && e.ActorId == actorId);
  }

  [Fact]
  public void PlaceSpace_OutsideFootprint_IsRejectedAndLeavesSpaceUnchanged()
  {
    var lot = CreateLot(Layout(20m, 10m));
    var space = AddSpace(lot, "A1");

    var result = Place(lot, space, Placement(25m, 5m));

    result.Status.ShouldBe(ResultStatus.Invalid);
    space.Placement.ShouldBeNull();
  }

  [Fact]
  public void PlaceSpace_OnMissingLevel_IsRejected()
  {
    var lot = CreateLot(Layout(levels: 2));
    var space = AddSpace(lot, "A1");

    Place(lot, space, Placement(5m, 5m, level: 1)).IsSuccess.ShouldBeTrue();
    Place(lot, space, Placement(5m, 5m, level: 2)).Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void PlaceSpace_WithNull_UnplacesTheSpace()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1", placement: Placement(5m, 5m));

    Place(lot, space, placement: null).IsSuccess.ShouldBeTrue();

    space.Placement.ShouldBeNull();
  }

  [Fact]
  public void UpdateSpace_EmptyZone_ClearsZoneAndWhitespaceIsTrimmed()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1", zone: "  East  ");
    space.Zone.ShouldBe("East");

    lot.UpdateSpace(space.Id, new SpaceUpdate(Zone: ""), actorId: null).IsSuccess.ShouldBeTrue();

    space.Zone.ShouldBeNull();
  }

  [Fact]
  public void UpdateSpace_TooLongZone_IsRejected()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");

    var result = lot.UpdateSpace(space.Id, new SpaceUpdate(Zone: new string('z', 51)), actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void AddSpace_PlacementOutsideLayout_ReturnsInvalidAndAddsNothing()
  {
    var lot = CreateLot(Layout(10m, 10m));
    lot.ClearDomainEvents();

    var result = lot.AddSpace("A1", SpaceType.General, actorId: null, placement: Placement(11m, 5m));

    result.Status.ShouldBe(ResultStatus.Invalid);
    result.ValidationErrors.ShouldContain(e => e.Identifier == "Placement");
    lot.Spaces.ShouldBeEmpty();
    lot.DomainEvents.ShouldBeEmpty();
  }

  [Fact]
  public void UpdateDetails_LayoutThatWouldStrandAPlacedSpace_IsRejected()
  {
    var lot = CreateLot();
    AddSpace(lot, "A1", placement: Placement(30m, 5m));

    var result = UpdateLayout(lot, Layout(20m, 20m));

    result.Status.ShouldBe(ResultStatus.Invalid);
    lot.Layout.ShouldBeNull();
  }

  [Fact]
  public void UpdateDetails_NullLayout_ClearsLayout()
  {
    var lot = CreateLot(Layout());

    UpdateLayout(lot, null).IsSuccess.ShouldBeTrue();

    lot.Layout.ShouldBeNull();
  }

  [Fact]
  public void ApplyLayout_PlacesAndClearsInOneBatch_AndRegistersSingleEvent()
  {
    var lot = CreateLot();
    var first = AddSpace(lot, "A1");
    var second = AddSpace(lot, "A2", placement: Placement(3m, 3m));
    lot.ClearDomainEvents();
    var actorId = Guid.NewGuid();

    var result = lot.ApplyLayout(Layout(), [
      new SpaceLayoutChange(first.Id, Placement(10m, 5m, 90m), "West"),
      new SpaceLayoutChange(second.Id, null, null),
    ], actorId);

    result.IsSuccess.ShouldBeTrue();
    first.Placement.ShouldBe(Placement(10m, 5m, 90m));
    first.Zone.ShouldBe("West");
    second.Placement.ShouldBeNull();
    lot.Layout.ShouldBe(Layout());

    var applied = lot.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<LotLayoutAppliedEvent>();
    applied.PlacedCount.ShouldBe(1);
    applied.ClearedCount.ShouldBe(1);
    applied.LayoutChanged.ShouldBeTrue();
    applied.ActorId.ShouldBe(actorId);
  }

  [Fact]
  public void ApplyLayout_OmittedZone_KeepsExistingZone()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1", zone: "Keep");

    lot.ApplyLayout(null, [new SpaceLayoutChange(space.Id, Placement(1m, 1m), null)], actorId: null)
      .IsSuccess.ShouldBeTrue();

    space.Zone.ShouldBe("Keep");
  }

  [Fact]
  public void ApplyLayout_ForeignSpaceId_RejectsWholeBatch()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");
    lot.ClearDomainEvents();

    var result = lot.ApplyLayout(null, [
      new SpaceLayoutChange(space.Id, Placement(1m, 1m), null),
      new SpaceLayoutChange(ParkingSpaceId.From(Guid.NewGuid()), Placement(2m, 2m), null),
    ], actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    space.Placement.ShouldBeNull();
    lot.DomainEvents.ShouldBeEmpty();
  }

  [Fact]
  public void ApplyLayout_DuplicateSpaceIds_AreRejected()
  {
    var lot = CreateLot();
    var space = AddSpace(lot, "A1");

    var result = lot.ApplyLayout(null, [
      new SpaceLayoutChange(space.Id, Placement(1m, 1m), null),
      new SpaceLayoutChange(space.Id, Placement(2m, 2m), null),
    ], actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    space.Placement.ShouldBeNull();
  }

  [Fact]
  public void ApplyLayout_OneRowOutsideNewFootprint_RejectsWholeBatch()
  {
    var lot = CreateLot();
    var inside = AddSpace(lot, "A1");
    var outside = AddSpace(lot, "A2");

    var result = lot.ApplyLayout(Layout(10m, 10m), [
      new SpaceLayoutChange(inside.Id, Placement(5m, 5m), null),
      new SpaceLayoutChange(outside.Id, Placement(15m, 5m), null),
    ], actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    inside.Placement.ShouldBeNull();
    lot.Layout.ShouldBeNull();
  }

  [Fact]
  public void ApplyLayout_NewFootprintStrandingAnUntouchedSpace_IsRejected()
  {
    var lot = CreateLot();
    AddSpace(lot, "A1", placement: Placement(50m, 5m));

    var result = lot.ApplyLayout(Layout(20m, 20m), [], actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }
}
