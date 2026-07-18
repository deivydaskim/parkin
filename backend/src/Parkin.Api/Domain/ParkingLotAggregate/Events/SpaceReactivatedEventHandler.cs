using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceReactivatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<SpaceReactivatedEvent>
{
  public async ValueTask Handle(SpaceReactivatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.SpaceReactivated,
      AuditEntityTypes.ParkingSpace,
      notification.SpaceId.Value,
      new { lotId = notification.LotId.Value });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
