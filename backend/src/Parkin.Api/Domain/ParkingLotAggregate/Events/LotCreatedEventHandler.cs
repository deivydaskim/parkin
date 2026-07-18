using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ParkingLotAggregate.Events;

public class LotCreatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<LotCreatedEvent>
{
  public async ValueTask Handle(LotCreatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.LotCreated,
      AuditEntityTypes.ParkingLot,
      notification.LotId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
