using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SpaceFeatures;

public record SpacePlacementRecord(
  decimal X,
  decimal Y,
  decimal RotationDegrees,
  int Level,
  decimal Width,
  decimal Length)
{
  public static SpacePlacementRecord? FromValue(SpacePlacement? placement) =>
    placement is null
      ? null
      : new(placement.X, placement.Y, placement.RotationDegrees, placement.Level, placement.Width, placement.Length);
}
