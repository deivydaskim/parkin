namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateReassignedEvent(PlateId plateId, DriverId fromDriverId, DriverId toDriverId, Guid? actorId)
  : DomainEventBase
{
  public PlateId PlateId { get; } = plateId;
  public DriverId FromDriverId { get; } = fromDriverId;
  public DriverId ToDriverId { get; } = toDriverId;
  public Guid? ActorId { get; } = actorId;
}
