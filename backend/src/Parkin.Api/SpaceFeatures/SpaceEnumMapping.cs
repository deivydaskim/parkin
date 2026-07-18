namespace Parkin.Api.SpaceFeatures;

internal static class SpaceEnumMapping
{
  public static SpaceRecord ToRecord(SpaceDto dto) => new(
    dto.Id.Value,
    dto.LotId.Value,
    dto.Label,
    dto.Type,
    dto.Status);
}
