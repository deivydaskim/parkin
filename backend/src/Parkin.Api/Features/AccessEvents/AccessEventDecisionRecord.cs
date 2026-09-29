using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Features.AccessEvents;

public record AccessEventDecisionRecord(
  Guid? EventId,
  Decision Decision,
  DenyReason? Reason,
  SessionPool? Pool,
  string? ReservedSpaceLabel,
  Guid? SessionId,
  DateTimeOffset OccurredAt);
