using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SpaceFeatures;

public record SpaceDto(
  ParkingSpaceId Id,
  ParkingLotId LotId,
  string Label,
  SpaceType Type,
  SpaceStatus Status,
  string? Zone,
  SpacePlacement? Placement)
{
  public static SpaceDto FromEntity(ParkingSpace space) =>
    new(space.Id, space.LotId, space.Label, space.Type, space.Status, space.Zone, space.Placement);
}
