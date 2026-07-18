using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.DriverAggregate.Events;

public class PlateReactivatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<PlateReactivatedEvent>
{
  public async ValueTask Handle(PlateReactivatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.PlateReactivated,
      AuditEntityTypes.Plate,
      notification.PlateId.Value,
      new { driverId = notification.DriverId.Value });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
