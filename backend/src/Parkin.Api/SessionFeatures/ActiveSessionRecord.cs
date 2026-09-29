using Parkin.Api.Domain.Services;

namespace Parkin.Api.SessionFeatures;

public record ActiveSessionRecord(
  Guid Id,
  string Plate,
  Guid? DriverId,
  string? DriverName,
  SessionPool Pool,
  Guid? SpaceId,
  string? SpaceLabel,
  DateTimeOffset EntryTime)
{
  public static ActiveSessionRecord FromDto(ActiveSessionDto dto) => new(
    dto.Id.Value,
    dto.Plate,
    dto.DriverId?.Value,
    dto.DriverName,
    dto.Pool,
    dto.SpaceId?.Value,
    dto.SpaceLabel,
    dto.EntryTime);
}
