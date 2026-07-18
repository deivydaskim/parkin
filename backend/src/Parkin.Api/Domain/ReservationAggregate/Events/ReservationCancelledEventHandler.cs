using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationCancelledEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<ReservationCancelledEvent>
{
  public async ValueTask Handle(ReservationCancelledEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.ReservationCancelled,
      AuditEntityTypes.Reservation,
      notification.ReservationId.Value);

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
