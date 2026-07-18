namespace Parkin.Api.PlateFeatures;

internal static class PlateMapping
{
  public static PlateRecord ToRecord(PlateDto dto) => new(
    dto.Id.Value,
    dto.DriverId.Value,
    dto.NormalizedPlateNumber,
    dto.Status);
}
