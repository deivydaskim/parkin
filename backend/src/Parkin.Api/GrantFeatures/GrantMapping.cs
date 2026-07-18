namespace Parkin.Api.GrantFeatures;

internal static class GrantMapping
{
  public static GrantRecord ToRecord(GrantDto dto) => new(
    dto.Id.Value,
    dto.DriverId.Value,
    dto.ParkingLotId.Value,
    dto.ValidFrom,
    dto.ValidTo,
    dto.Status);
}
