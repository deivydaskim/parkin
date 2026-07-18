using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SpaceFeatures;

public record SpaceDto(
  ParkingSpaceId Id,
  ParkingLotId LotId,
  string Label,
  SpaceType Type,
  SpaceStatus Status);
