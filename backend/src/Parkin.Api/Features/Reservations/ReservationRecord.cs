using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.Features.Reservations;

public record ReservationRecord(
  Guid Id,
  Guid SpaceId,
  Guid DriverId,
  Guid LotId,
  ReservationStatus Status);
