using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.AccessEventFeatures.List;

public record AccessEventListItemRecord(
  Guid Id,
  string Plate,
  Direction Direction,
  Decision Decision,
  DenyReason? Reason,
  SessionPool? Pool,
  string? ReservedSpaceLabel,
  EventSource Source,
  Guid? DriverId,
  string? DriverName,
  DateTimeOffset OccurredAt)
{
  public static AccessEventListItemRecord FromDto(AccessEventListItemDto dto) => new(
    dto.Id.Value,
    dto.Plate,
    dto.Direction,
    dto.Decision,
    dto.Reason,
    dto.Pool,
    dto.ReservedSpaceLabel,
    dto.Source,
    dto.DriverId?.Value,
    dto.DriverName,
    dto.OccurredAt);
}
