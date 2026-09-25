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
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);

    var result = lot.PlaceSpace(space.Id, Placement(500m, 250m, 90m, level: 3), "North", actorId: null);

    result.IsSuccess.ShouldBeTrue();
    space.Placement.ShouldBe(Placement(500m, 250m, 90m, level: 3));
    space.Zone.ShouldBe("North");
  }

  [Fact]
  public void PlaceSpace_RegistersSpaceUpdatedEvent()
  {
    var lot = CreateLot();
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);
    var actorId = Guid.NewGuid();

    lot.PlaceSpace(space.Id, Placement(5m, 5m), zone: null, actorId);

    lot.DomainEvents.OfType<SpaceUpdatedEvent>()
      .ShouldContain(e => e.SpaceId == space.Id && e.ActorId == actorId);
  }

  [Fact]
  public void PlaceSpace_OutsideFootprint_IsRejectedAndLeavesSpaceUnchanged()
  {
    var lot = CreateLot(Layout(20m, 10m));
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);

    var result = lot.PlaceSpace(space.Id, Placement(25m, 5m), zone: null, actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    space.Placement.ShouldBeNull();
  }

  [Fact]
  public void PlaceSpace_OnMissingLevel_IsRejected()
  {
    var lot = CreateLot(Layout(levels: 2));
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);

    lot.PlaceSpace(space.Id, Placement(5m, 5m, level: 1), zone: null, actorId: null).IsSuccess.ShouldBeTrue();
    lot.PlaceSpace(space.Id, Placement(5m, 5m, level: 2), zone: null, actorId: null)
      .Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void PlaceSpace_WithNull_UnplacesTheSpace()
  {
    var lot = CreateLot();
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null, placement: Placement(5m, 5m));

    lot.PlaceSpace(space.Id, placement: null, zone: null, actorId: null).IsSuccess.ShouldBeTrue();

    space.Placement.ShouldBeNull();
  }

  [Fact]
  public void UpdateSpace_EmptyZone_ClearsZoneAndWhitespaceIsTrimmed()
  {
    var lot = CreateLot();
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null, zone: "  East  ");
    space.Zone.ShouldBe("East");

    lot.UpdateSpace(space.Id, new SpaceUpdate(Zone: ""), actorId: null).IsSuccess.ShouldBeTrue();

    space.Zone.ShouldBeNull();
  }

  [Fact]
  public void UpdateSpace_TooLongZone_IsRejected()
  {
    var lot = CreateLot();
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);

    var result = lot.UpdateSpace(space.Id, new SpaceUpdate(Zone: new string('z', 51)), actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Fact]
  public void AddSpace_PlacementOutsideLayout_Throws()
  {
    var lot = CreateLot(Layout(10m, 10m));

    Should.Throw<ArgumentException>(() =>
      lot.AddSpace("A1", SpaceType.General, actorId: null, placement: Placement(11m, 5m)));
  }

  [Fact]
  public void SetLayout_ThatWouldStrandAPlacedSpace_IsRejected()
  {
    var lot = CreateLot();
    lot.AddSpace("A1", SpaceType.General, actorId: null, placement: Placement(30m, 5m));

    var result = lot.SetLayout(Layout(20m, 20m));

    result.Status.ShouldBe(ResultStatus.Invalid);
    lot.Layout.ShouldBeNull();
  }

  [Fact]
  public void SetLayout_Null_ClearsLayout()
  {
    var lot = CreateLot(Layout());

    lot.SetLayout(null).IsSuccess.ShouldBeTrue();

    lot.Layout.ShouldBeNull();
  }

  [Fact]
  public void ApplyLayout_PlacesAndClearsInOneBatch_AndRegistersSingleEvent()
  {
    var lot = CreateLot();
    var first = lot.AddSpace("A1", SpaceType.General, actorId: null);
    var second = lot.AddSpace("A2", SpaceType.General, actorId: null, placement: Placement(3m, 3m));
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
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null, zone: "Keep");

    lot.ApplyLayout(null, [new SpaceLayoutChange(space.Id, Placement(1m, 1m), null)], actorId: null)
      .IsSuccess.ShouldBeTrue();

    space.Zone.ShouldBe("Keep");
  }

  [Fact]
  public void ApplyLayout_ForeignSpaceId_RejectsWholeBatch()
  {
    var lot = CreateLot();
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);
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
    var space = lot.AddSpace("A1", SpaceType.General, actorId: null);

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
    var inside = lot.AddSpace("A1", SpaceType.General, actorId: null);
    var outside = lot.AddSpace("A2", SpaceType.General, actorId: null);

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
    lot.AddSpace("A1", SpaceType.General, actorId: null, placement: Placement(50m, 5m));

    var result = lot.ApplyLayout(Layout(20m, 20m), [], actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
  }
}
