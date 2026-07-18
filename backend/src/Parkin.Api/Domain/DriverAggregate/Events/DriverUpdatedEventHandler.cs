using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverUpdatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<DriverUpdatedEvent>
{
  public async ValueTask Handle(DriverUpdatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.DriverUpdated,
      AuditEntityTypes.Driver,
      notification.DriverId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
