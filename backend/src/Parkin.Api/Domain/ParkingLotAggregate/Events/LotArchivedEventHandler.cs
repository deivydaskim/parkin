using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotArchivedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<LotArchivedEvent>
{
  public async ValueTask Handle(LotArchivedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      AuditActorType.System,
      null,
      "lot.archived",
      "ParkingLot",
      notification.LotId.Value,
      null);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
