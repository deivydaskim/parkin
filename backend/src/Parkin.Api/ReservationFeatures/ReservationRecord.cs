using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.ReservationFeatures;

public record ReservationRecord(
  Guid Id,
  Guid SpaceId,
  Guid DriverId,
  Guid LotId,
  ReservationStatus Status);
