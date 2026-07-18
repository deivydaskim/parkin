using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ApiKeyAggregate.Events;

public class ApiKeyRevokedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<ApiKeyRevokedEvent>
{
  public async ValueTask Handle(ApiKeyRevokedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.ApiKeyRevoke,
      AuditEntityTypes.ApiKey,
      notification.ApiKeyId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
