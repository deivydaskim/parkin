using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.Features.Reservations;

public record ReservationResponse(
  Guid Id,
  Guid SpaceId,
  Guid DriverId,
  Guid LotId,
  ReservationStatus Status)
{
  public static ReservationResponse From(Reservation reservation)
    => new(reservation.Id.Value, reservation.SpaceId.Value, reservation.DriverId.Value, reservation.LotId.Value,
      reservation.Status);
}
