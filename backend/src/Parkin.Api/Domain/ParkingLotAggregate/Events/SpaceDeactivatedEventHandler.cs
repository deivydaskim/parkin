using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceDeactivatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<SpaceDeactivatedEvent>
{
  public async ValueTask Handle(SpaceDeactivatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.SpaceDeactivated,
      AuditEntityTypes.ParkingSpace,
      notification.SpaceId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
