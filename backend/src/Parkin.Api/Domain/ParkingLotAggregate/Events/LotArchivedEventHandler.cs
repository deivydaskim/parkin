using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotArchivedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<LotArchivedEvent>
{
  public async ValueTask Handle(LotArchivedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.LotArchived,
      AuditEntityTypes.ParkingLot,
      notification.LotId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
