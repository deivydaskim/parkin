using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceUpdatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<SpaceUpdatedEvent>
{
  public async ValueTask Handle(SpaceUpdatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.SpaceUpdated,
      AuditEntityTypes.ParkingSpace,
      notification.SpaceId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
