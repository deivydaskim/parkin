using Ardalis.Result;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.DriverAggregate;

public class DriverPlateTests
{
  private static readonly PlateId UnknownPlateId = PlateId.From(Guid.NewGuid());

  private static Driver CreateDriver(string name = "Jane Driver") => Driver.Create(name, null, actorId: null);

  [Fact]
  public void AddPlate_NormalizesNumberAndRegistersEvent()
  {
    var driver = CreateDriver();

    var plate = driver.AddPlate(" abc 123 ", actorId: null);

    plate.NormalizedPlateNumber.ShouldBe("ABC123");
    plate.DriverId.ShouldBe(driver.Id);
    plate.Status.ShouldBe(PlateStatus.Active);
    driver.Plates.ShouldHaveSingleItem().ShouldBe(plate);
    driver.DomainEvents.OfType<PlateAddedEvent>().ShouldHaveSingleItem().PlateId.ShouldBe(plate.Id);
  }

  [Fact]
  public void DeactivatePlate_KnownPlate_DeactivatesAndRegistersEvent()
  {
    var driver = CreateDriver();
    var plate = driver.AddPlate("AAA111", actorId: null);

    var result = driver.DeactivatePlate(plate.Id, actorId: null);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldBe(plate);
    plate.Status.ShouldBe(PlateStatus.Inactive);
    driver.DomainEvents.OfType<PlateDeactivatedEvent>().ShouldHaveSingleItem().PlateId.ShouldBe(plate.Id);
  }

  [Fact]
  public void DeactivatePlate_UnknownPlate_ReturnsNotFound()
  {
    var driver = CreateDriver();

    var result = driver.DeactivatePlate(UnknownPlateId, actorId: null);

    result.Status.ShouldBe(ResultStatus.NotFound);
    driver.DomainEvents.OfType<PlateDeactivatedEvent>().ShouldBeEmpty();
  }

  [Fact]
  public void ReactivatePlate_KnownPlate_ReactivatesAndRegistersEvent()
  {
    var driver = CreateDriver();
    var plate = driver.AddPlate("AAA111", actorId: null);
    driver.DeactivatePlate(plate.Id, actorId: null);

    var result = driver.ReactivatePlate(plate.Id, actorId: null);

    result.IsSuccess.ShouldBeTrue();
    plate.Status.ShouldBe(PlateStatus.Active);
    driver.DomainEvents.OfType<PlateReactivatedEvent>().ShouldHaveSingleItem().PlateId.ShouldBe(plate.Id);
  }

  [Fact]
  public void ReactivatePlate_UnknownPlate_ReturnsNotFound()
  {
    var driver = CreateDriver();

    var result = driver.ReactivatePlate(UnknownPlateId, actorId: null);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public void TransferPlate_ToOtherDriver_MovesPlateAndRegistersEventOnTarget()
  {
    var source = CreateDriver("Source");
    var target = CreateDriver("Target");
    var plate = source.AddPlate("AAA111", actorId: null);
    var actorId = Guid.NewGuid();

    var result = source.TransferPlate(plate.Id, target, actorId);

    result.IsSuccess.ShouldBeTrue();
    source.Plates.ShouldBeEmpty();
    target.Plates.ShouldHaveSingleItem().ShouldBe(plate);
    plate.DriverId.ShouldBe(target.Id);
    var reassigned = target.DomainEvents.OfType<PlateReassignedEvent>().ShouldHaveSingleItem();
    reassigned.PlateId.ShouldBe(plate.Id);
    reassigned.FromDriverId.ShouldBe(source.Id);
    reassigned.ToDriverId.ShouldBe(target.Id);
    reassigned.ActorId.ShouldBe(actorId);
  }

  [Fact]
  public void TransferPlate_UnknownPlate_ReturnsNotFound()
  {
    var source = CreateDriver("Source");
    var target = CreateDriver("Target");

    var result = source.TransferPlate(UnknownPlateId, target, actorId: null);

    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public void TransferPlate_ToSameDriver_ReturnsInvalidAndKeepsPlate()
  {
    var driver = CreateDriver();
    var plate = driver.AddPlate("AAA111", actorId: null);

    var result = driver.TransferPlate(plate.Id, driver, actorId: null);

    result.Status.ShouldBe(ResultStatus.Invalid);
    driver.Plates.ShouldHaveSingleItem().ShouldBe(plate);
    driver.DomainEvents.OfType<PlateReassignedEvent>().ShouldBeEmpty();
  }
}
