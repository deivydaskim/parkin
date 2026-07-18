using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotRestoredEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<LotRestoredEvent>
{
  public async ValueTask Handle(LotRestoredEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.LotRestored,
      AuditEntityTypes.ParkingLot,
      notification.LotId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
