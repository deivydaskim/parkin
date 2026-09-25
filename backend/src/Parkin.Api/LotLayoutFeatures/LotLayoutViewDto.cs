using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.LotLayoutFeatures;

public record LotLayoutViewDto(
  ParkingLotId LotId,
  string Name,
  LotStatus Status,
  LotLayout? Layout,
  IReadOnlyList<LotLayoutSpaceDto> Spaces);

public record LotLayoutSpaceDto(
  ParkingSpaceId Id,
  string Label,
  SpaceType Type,
  SpaceStatus Status,
  string? Zone,
  SpacePlacement? Placement,
  LotLayoutReservationDto? Reservation);

public record LotLayoutReservationDto(ReservationId Id, DriverId DriverId, string DriverName);
