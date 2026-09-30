using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverArchivedEvent(DriverId driverId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.DriverArchived, AuditEntityTypes.Driver, driverId.Value)
{
  public DriverId DriverId { get; } = driverId;
}
