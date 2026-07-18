using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.ReservationFeatures;

public record ReservationDto(
  ReservationId Id,
  ParkingSpaceId SpaceId,
  DriverId DriverId,
  ParkingLotId LotId,
  ReservationStatus Status);
