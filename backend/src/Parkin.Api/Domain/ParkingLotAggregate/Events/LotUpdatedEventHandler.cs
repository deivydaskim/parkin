using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotUpdatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<LotUpdatedEvent>
{
  public async ValueTask Handle(LotUpdatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.LotUpdated,
      AuditEntityTypes.ParkingLot,
      notification.LotId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
