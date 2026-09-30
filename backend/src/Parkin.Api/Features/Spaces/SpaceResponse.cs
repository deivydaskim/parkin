using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Spaces;

public record SpaceResponse(
  Guid Id,
  Guid LotId,
  string Label,
  SpaceType Type,
  SpaceStatus Status,
  string? Zone,
  SpacePlacementResponse? Placement,
  Guid? ReservedDriverId,
  string? ReservedDriverName)
{
  public static SpaceResponse From(ParkingSpace space) =>
    new(space.Id.Value, space.LotId.Value, space.Label, space.Type, space.Status, space.Zone,
      SpacePlacementResponse.From(space.Placement), ReservedDriverId: null, ReservedDriverName: null);
}

public record SpacePlacementResponse(
  decimal X,
  decimal Y,
  decimal RotationDegrees,
  int Level,
  decimal Width,
  decimal Length)
{
  public static SpacePlacementResponse? From(SpacePlacement? placement) =>
    placement is null
      ? null
      : new(placement.X, placement.Y, placement.RotationDegrees, placement.Level, placement.Width, placement.Length);
}
