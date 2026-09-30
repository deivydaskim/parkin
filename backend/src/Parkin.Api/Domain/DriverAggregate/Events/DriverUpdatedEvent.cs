using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverUpdatedEvent(DriverId driverId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.DriverUpdated, AuditEntityTypes.Driver, driverId.Value)
{
  public DriverId DriverId { get; } = driverId;
}
