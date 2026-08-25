using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.AccessEventFeatures;

// EventId is null only for a DENY LotNotFound, the one outcome that is never persisted.
public record AccessEventDecisionDto(
  AccessEventId? EventId,
  Decision Decision,
  DenyReason? Reason,
  SessionPool? Pool,
  string? ReservedSpaceLabel,
  ParkingSessionId? SessionId,
  DateTimeOffset OccurredAt);
