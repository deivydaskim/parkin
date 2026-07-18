using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateDeactivatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<PlateDeactivatedEvent>
{
  public async ValueTask Handle(PlateDeactivatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.PlateDeactivated,
      AuditEntityTypes.Plate,
      notification.PlateId.Value,
      new { driverId = notification.DriverId.Value });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
