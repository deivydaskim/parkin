using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverRestoredEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<DriverRestoredEvent>
{
  public async ValueTask Handle(DriverRestoredEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.DriverRestored,
      AuditEntityTypes.Driver,
      notification.DriverId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
