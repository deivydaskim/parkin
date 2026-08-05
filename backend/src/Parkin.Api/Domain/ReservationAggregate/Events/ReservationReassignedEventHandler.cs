using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationReassignedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<ReservationReassignedEvent>
{
  public async ValueTask Handle(ReservationReassignedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.ReservationReassigned,
      AuditEntityTypes.Reservation,
      notification.NewReservationId.Value,
      new
      {
        previousReservationId = notification.PreviousReservationId.Value,
        spaceId = notification.SpaceId.Value,
        lotId = notification.LotId.Value,
        previousDriverId = notification.PreviousDriverId.Value,
        newDriverId = notification.NewDriverId.Value
      });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
