using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateReassignedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<PlateReassignedEvent>
{
  public async ValueTask Handle(PlateReassignedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.PlateReassigned,
      AuditEntityTypes.Plate,
      notification.PlateId.Value,
      new { fromDriverId = notification.FromDriverId.Value, toDriverId = notification.ToDriverId.Value });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
