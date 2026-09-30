using Parkin.Api.Domain.Exceptions;
using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.Features.Reservations;

internal static class ReservationConflicts
{
  public const string SpaceAlreadyReserved = "Space already has an active reservation";
  public const string DriverAlreadyReservedInLot = "Driver already has an active reservation in this lot";

  public static string? MessageFor(UniqueConstraintViolationException exception) => exception.ConstraintName switch
  {
    Reservation.ActiveSpaceIndex => SpaceAlreadyReserved,
    Reservation.ActiveDriverLotIndex => DriverAlreadyReservedInLot,
    _ => null
  };
}
