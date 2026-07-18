using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class DriverCreatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<DriverCreatedEvent>
{
  public async ValueTask Handle(DriverCreatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.DriverCreated,
      AuditEntityTypes.Driver,
      notification.DriverId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
