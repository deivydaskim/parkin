using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Events;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain;

public class LifecycleIdempotencyTests
{
  [Fact]
  public void ArchivingAnArchivedLot_RaisesNoSecondEvent()
  {
    var lot = ParkingLot.Create("Idempotent Lot", "Europe/Vilnius");
    lot.Archive(actorId: null);
    lot.Archive(actorId: null);

    lot.Status.ShouldBe(LotStatus.Archived);
    lot.DomainEvents.OfType<LotArchivedEvent>().Count().ShouldBe(1);
  }

  [Fact]
  public void RestoringAnActiveLot_RaisesNoEvent()
  {
    var lot = ParkingLot.Create("Active Lot", "Europe/Vilnius");

    lot.Restore(actorId: null);

    lot.DomainEvents.OfType<LotRestoredEvent>().ShouldBeEmpty();
  }

  [Fact]
  public void ArchivingAnArchivedDriver_RaisesNoSecondEvent()
  {
    var driver = Driver.Create("Jane Driver", null, actorId: null);
    driver.Archive(actorId: null);
    driver.Archive(actorId: null);

    driver.DomainEvents.OfType<DriverArchivedEvent>().Count().ShouldBe(1);
  }

  [Fact]
  public void RestoringAnActiveDriver_RaisesNoEvent()
  {
    var driver = Driver.Create("Jane Driver", null, actorId: null);

    driver.Restore(actorId: null);

    driver.DomainEvents.OfType<DriverRestoredEvent>().ShouldBeEmpty();
  }

  [Fact]
  public void DeactivatingAnInactivePlate_SucceedsWithoutSecondEvent()
  {
    var driver = Driver.Create("Jane Driver", null, actorId: null);
    var plate = driver.AddPlate("AAA111", actorId: null);
    driver.DeactivatePlate(plate.Id, actorId: null);

    var result = driver.DeactivatePlate(plate.Id, actorId: null);

    result.IsSuccess.ShouldBeTrue();
    driver.DomainEvents.OfType<PlateDeactivatedEvent>().Count().ShouldBe(1);
  }

  [Fact]
  public void ReactivatingAnActivePlate_SucceedsWithoutEvent()
  {
    var driver = Driver.Create("Jane Driver", null, actorId: null);
    var plate = driver.AddPlate("AAA111", actorId: null);

    var result = driver.ReactivatePlate(plate.Id, actorId: null);

    result.IsSuccess.ShouldBeTrue();
    driver.DomainEvents.OfType<PlateReactivatedEvent>().ShouldBeEmpty();
  }
}
