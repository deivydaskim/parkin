using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SpaceFeatures;

public record SpaceRecord(
  Guid Id,
  Guid LotId,
  string Label,
  SpaceType Type,
  SpaceStatus Status);
