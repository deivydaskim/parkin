using Parkin.Api.LotFeatures;
using Parkin.Api.SpaceFeatures;

namespace Parkin.Api.LotLayoutFeatures;

internal static class LotLayoutMapping
{
  public static LotLayoutViewRecord ToRecord(LotLayoutViewDto dto) => new(
    new LotLayoutLotRecord(dto.LotId.Value, dto.Name, dto.Status, LotLayoutRecord.FromValue(dto.Layout)),
    dto.Spaces.Select(ToRecord).ToList());

  private static LotLayoutSpaceRecord ToRecord(LotLayoutSpaceDto space) => new(
    space.Id.Value,
    space.Label,
    space.Type,
    space.Status,
    space.Zone,
    SpacePlacementRecord.FromValue(space.Placement),
    space.Reservation is null
      ? null
      : new LotLayoutReservationRecord(
        space.Reservation.Id.Value, space.Reservation.DriverId.Value, space.Reservation.DriverName));
}
