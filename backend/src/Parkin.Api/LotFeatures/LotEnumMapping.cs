namespace Parkin.Api.LotFeatures;

internal static class LotEnumMapping
{
  // Capacity is derived from active ParkingSpace rows, which don't exist until T2.2.
  public static LotRecord ToRecord(LotDto dto) => new(
    dto.Id.Value,
    dto.Name,
    dto.Address,
    dto.Timezone,
    dto.AccessMode,
    dto.FullBehavior,
    dto.Status,
    Capacity: 0);
}
