using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverCreatedEvent(DriverId driverId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.DriverCreated, AuditEntityTypes.Driver, driverId.Value)
{
  public DriverId DriverId { get; } = driverId;
}
