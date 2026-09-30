using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationCancelledEvent(ReservationId reservationId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.ReservationCancelled, AuditEntityTypes.Reservation, reservationId.Value)
{
  public ReservationId ReservationId { get; } = reservationId;
}
