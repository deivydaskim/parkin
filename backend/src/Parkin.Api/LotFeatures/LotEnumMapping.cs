namespace Parkin.Api.LotFeatures;

internal static class LotEnumMapping
{
  public static LotRecord ToRecord(LotDto dto) => new(
    dto.Id.Value,
    dto.Name,
    dto.Address,
    dto.Timezone,
    dto.AccessMode,
    dto.FullBehavior,
    dto.Status,
    dto.Capacity);
}
