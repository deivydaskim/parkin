namespace Parkin.Api.Features.Grants;

internal static class GrantMapping
{
  public static GrantRecord ToRecord(GrantDto dto) => new(
    dto.Id.Value,
    dto.DriverId.Value,
    dto.ParkingLotId.Value,
    dto.ValidFrom,
    dto.ValidTo,
    dto.Status,
    dto.ParkingLotName,
    dto.DriverName);
}
