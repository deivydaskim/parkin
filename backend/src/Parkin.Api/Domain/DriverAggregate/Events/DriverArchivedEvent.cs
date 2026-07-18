namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverArchivedEvent(DriverId driverId, Guid? actorId) : DomainEventBase
{
  public DriverId DriverId { get; } = driverId;
  public Guid? ActorId { get; } = actorId;
}
