using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate.Events;

public class GrantCreatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<GrantCreatedEvent>
{
  public async ValueTask Handle(GrantCreatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.GrantCreated,
      AuditEntityTypes.AccessGrant,
      notification.GrantId.Value,
      new { driverId = notification.DriverId.Value, lotId = notification.LotId.Value });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
