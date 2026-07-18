using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ApiKeyAggregate.Events;

public class ApiKeyCreatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<ApiKeyCreatedEvent>
{
  public async ValueTask Handle(ApiKeyCreatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.ApiKeyCreate,
      AuditEntityTypes.ApiKey,
      notification.ApiKeyId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
