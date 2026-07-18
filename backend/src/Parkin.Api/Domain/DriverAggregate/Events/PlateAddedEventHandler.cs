using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateAddedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<PlateAddedEvent>
{
  public async ValueTask Handle(PlateAddedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.PlateAdded,
      AuditEntityTypes.Plate,
      notification.PlateId.Value,
      new { driverId = notification.DriverId.Value });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
