namespace Parkin.Api.Domain.ReservationAggregate.Events;

public class ReservationCancelledEvent(ReservationId reservationId, Guid? actorId) : DomainEventBase
{
  public ReservationId ReservationId { get; } = reservationId;
  public Guid? ActorId { get; } = actorId;
}
