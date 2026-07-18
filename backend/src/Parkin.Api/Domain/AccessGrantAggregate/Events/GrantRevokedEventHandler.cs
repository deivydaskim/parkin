using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate.Events;

public class GrantRevokedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<GrantRevokedEvent>
{
  public async ValueTask Handle(GrantRevokedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.GrantRevoked,
      AuditEntityTypes.AccessGrant,
      notification.GrantId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
