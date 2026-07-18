using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverArchivedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<DriverArchivedEvent>
{
  public async ValueTask Handle(DriverArchivedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.DriverArchived,
      AuditEntityTypes.Driver,
      notification.DriverId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
