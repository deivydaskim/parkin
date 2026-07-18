using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class SpaceCreatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<SpaceCreatedEvent>
{
  public async ValueTask Handle(SpaceCreatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.SpaceCreated,
      AuditEntityTypes.ParkingSpace,
      notification.SpaceId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
