using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.ReservationAggregate.Specifications;

public class ActiveReservationByDriverLotSpec : Specification<Reservation>
{
  public ActiveReservationByDriverLotSpec(DriverId driverId, ParkingLotId lotId) =>
    Query
        .Where(reservation =>
          reservation.DriverId == driverId &&
          reservation.LotId == lotId &&
          reservation.Status == ReservationStatus.Active);
}
