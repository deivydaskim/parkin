using System.Text.Json;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.ParkingLotAggregate;

public class ParkingLotDetailsTests
{
  private static readonly Guid ActorId = Guid.NewGuid();

  private static ParkingLot CreateLot()
  {
    var lot = ParkingLot.Create("North Garage", "Europe/Vilnius", "1 Main St",
      AccessMode.Restricted, FullBehavior.Block);
    lot.ClearDomainEvents();
    return lot;
  }

  [Fact]
  public void UpdateDetails_WithSameValues_RaisesNoEvent()
  {
    var lot = CreateLot();

    var result = lot.UpdateDetails(lot.Name, lot.Address, lot.Timezone, lot.Layout, ActorId);

    result.IsSuccess.ShouldBeTrue();
    lot.DomainEvents.ShouldBeEmpty();
  }

  [Fact]
  public void UpdateDetails_RecordsOnlyChangedFields()
  {
    var lot = CreateLot();
    var layout = LotLayout.Create(40m, 30m, 2).Value;

    lot.UpdateDetails("South Garage", lot.Address, lot.Timezone, layout, ActorId);

    var updated = lot.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<LotUpdatedEvent>();
    updated.ActorId.ShouldBe(ActorId);
    updated.Action.ShouldBe(AuditActions.LotUpdated);
    updated.Changes.Keys.ShouldBe(["name", "layout"], ignoreOrder: true);
    updated.Changes["name"].ShouldBe(new LotFieldChange("North Garage", "South Garage"));
    updated.Changes["layout"].ShouldBe(new LotFieldChange(null, layout.ToString()));
    lot.Name.ShouldBe("South Garage");
    lot.Layout.ShouldBe(layout);
  }

  [Fact]
  public void UpdateDetails_MetadataSerializesFromAndTo()
  {
    var lot = CreateLot();

    lot.UpdateDetails(lot.Name, address: null, lot.Timezone, lot.Layout, ActorId);

    var updated = lot.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<LotUpdatedEvent>();
    JsonSerializer.Serialize(updated.Metadata)
      .ShouldBe("""{"changes":{"address":{"from":"1 Main St","to":null}}}""");
  }

  [Fact]
  public void SetAccessMode_ToCurrentMode_RaisesNoEvent()
  {
    var lot = CreateLot();

    lot.SetAccessMode(AccessMode.Restricted, ActorId);

    lot.DomainEvents.ShouldBeEmpty();
  }

  [Fact]
  public void SetAccessMode_RestrictedToOpen_RaisesDedicatedAuditEvent()
  {
    var lot = CreateLot();

    lot.SetAccessMode(AccessMode.Open, ActorId);

    lot.AccessMode.ShouldBe(AccessMode.Open);
    var changed = lot.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<LotAccessModeChangedEvent>();
    changed.Action.ShouldBe(AuditActions.LotAccessModeChanged);
    changed.EntityId.ShouldBe(lot.Id.Value);
    changed.ActorId.ShouldBe(ActorId);
    changed.From.ShouldBe(AccessMode.Restricted);
    changed.To.ShouldBe(AccessMode.Open);
    JsonSerializer.Serialize(changed.Metadata).ShouldBe("""{"from":"Restricted","to":"Open"}""");
  }

  [Fact]
  public void SetFullBehavior_ToCurrentBehavior_RaisesNoEvent()
  {
    var lot = CreateLot();

    lot.SetFullBehavior(FullBehavior.Block, ActorId);

    lot.DomainEvents.ShouldBeEmpty();
  }

  [Fact]
  public void SetFullBehavior_Change_RaisesDedicatedAuditEvent()
  {
    var lot = CreateLot();

    lot.SetFullBehavior(FullBehavior.AllowOverflow, ActorId);

    lot.FullBehavior.ShouldBe(FullBehavior.AllowOverflow);
    var changed = lot.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<LotFullBehaviorChangedEvent>();
    changed.Action.ShouldBe(AuditActions.LotFullBehaviorChanged);
    changed.From.ShouldBe(FullBehavior.Block);
    changed.To.ShouldBe(FullBehavior.AllowOverflow);
    JsonSerializer.Serialize(changed.Metadata).ShouldBe("""{"from":"Block","to":"AllowOverflow"}""");
  }
}
