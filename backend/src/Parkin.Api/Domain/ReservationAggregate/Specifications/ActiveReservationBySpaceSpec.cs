using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Specifications;

public class ActiveReservationBySpaceSpec : Specification<Reservation>
{
  public ActiveReservationBySpaceSpec(ParkingSpaceId spaceId) =>
    Query
        .Where(reservation =>
          reservation.SpaceId == spaceId &&
          reservation.Status == ReservationStatus.Active);
}
