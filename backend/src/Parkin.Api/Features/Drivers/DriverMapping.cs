namespace Parkin.Api.Features.Drivers;

internal static class DriverMapping
{
  public static DriverRecord ToRecord(DriverDto dto) => new(
    dto.Id.Value,
    dto.Name,
    dto.Contact,
    dto.Status,
    dto.PlateCount);
}
