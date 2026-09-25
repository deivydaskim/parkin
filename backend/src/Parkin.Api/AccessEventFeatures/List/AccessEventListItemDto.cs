using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.AccessEventFeatures.List;

public record AccessEventListItemDto(
  AccessEventId Id,
  string Plate,
  Direction Direction,
  Decision Decision,
  DenyReason? Reason,
  SessionPool? Pool,
  string? ReservedSpaceLabel,
  EventSource Source,
  DriverId? DriverId,
  string? DriverName,
  DateTimeOffset OccurredAt);
