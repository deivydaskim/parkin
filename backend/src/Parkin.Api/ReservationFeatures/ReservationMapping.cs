namespace Parkin.Api.ReservationFeatures;

internal static class ReservationMapping
{
  public static ReservationRecord ToRecord(ReservationDto dto) => new(
    dto.Id.Value,
    dto.SpaceId.Value,
    dto.DriverId.Value,
    dto.LotId.Value,
    dto.Status);
}
