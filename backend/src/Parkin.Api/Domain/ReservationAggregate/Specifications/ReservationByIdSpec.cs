namespace Parkin.Api.Domain.ReservationAggregate.Specifications;

public class ReservationByIdSpec : Specification<Reservation>
{
  public ReservationByIdSpec(ReservationId reservationId) =>
    Query
        .Where(reservation => reservation.Id == reservationId);
}
