using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateReassignedEvent(PlateId plateId, DriverId fromDriverId, DriverId toDriverId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.PlateReassigned, AuditEntityTypes.Plate, plateId.Value)
{
  public PlateId PlateId { get; } = plateId;
  public DriverId FromDriverId { get; } = fromDriverId;
  public DriverId ToDriverId { get; } = toDriverId;

  public override object Metadata => new { fromDriverId = FromDriverId.Value, toDriverId = ToDriverId.Value };
}
