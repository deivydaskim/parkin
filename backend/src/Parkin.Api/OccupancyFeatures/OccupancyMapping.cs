namespace Parkin.Api.OccupancyFeatures;

internal static class OccupancyMapping
{
  public static LotOccupancyRecord ToRecord(LotOccupancyDto dto) => new(
    dto.LotId.Value,
    dto.GeneralCapacity,
    dto.GeneralUsed,
    dto.GeneralFree,
    dto.IsGeneralPoolFull,
    dto.IsOverCapacity,
    dto.ReservedSpaceCount,
    dto.ReservedOccupied,
    dto.AsOf,
    dto.LotName);
}
