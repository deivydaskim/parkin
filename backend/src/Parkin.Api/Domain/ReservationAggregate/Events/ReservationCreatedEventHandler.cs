using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationCreatedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<ReservationCreatedEvent>
{
  public async ValueTask Handle(ReservationCreatedEvent notification, CancellationToken cancellationToken)
  {
    var entry = AuditLogEntry.Create(
      notification.ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System,
      notification.ActorId,
      AuditActions.ReservationCreated,
      AuditEntityTypes.Reservation,
      notification.ReservationId.Value,
      new { spaceId = notification.SpaceId.Value, driverId = notification.DriverId.Value, lotId = notification.LotId.Value });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
