using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateDeactivatedEvent(DriverId driverId, PlateId plateId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.PlateDeactivated, AuditEntityTypes.Plate, plateId.Value)
{
  public DriverId DriverId { get; } = driverId;
  public PlateId PlateId { get; } = plateId;

  public override object Metadata => new { driverId = DriverId.Value };
}
