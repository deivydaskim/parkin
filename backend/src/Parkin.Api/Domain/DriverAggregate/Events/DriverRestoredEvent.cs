using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverRestoredEvent(DriverId driverId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.DriverRestored, AuditEntityTypes.Driver, driverId.Value)
{
  public DriverId DriverId { get; } = driverId;
}
