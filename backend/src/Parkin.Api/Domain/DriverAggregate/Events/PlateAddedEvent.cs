namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateAddedEvent(DriverId driverId, PlateId plateId, Guid? actorId) : DomainEventBase
{
  public DriverId DriverId { get; } = driverId;
  public PlateId PlateId { get; } = plateId;
  public Guid? ActorId { get; } = actorId;
}
