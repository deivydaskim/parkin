using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.AccessEventFeatures;

public record AccessEventDecisionRecord(
  Guid? EventId,
  Decision Decision,
  DenyReason? Reason,
  SessionPool? Pool,
  string? ReservedSpaceLabel,
  Guid? SessionId,
  DateTimeOffset OccurredAt);
