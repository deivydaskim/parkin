using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotLayoutAppliedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<LotLayoutAppliedEvent>
{
  public async ValueTask Handle(LotLayoutAppliedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.LotLayoutApplied,
      AuditEntityTypes.ParkingLot,
      notification.LotId.Value,
      new
      {
        placed = notification.PlacedCount,
        cleared = notification.ClearedCount,
        layoutChanged = notification.LayoutChanged,
      });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
