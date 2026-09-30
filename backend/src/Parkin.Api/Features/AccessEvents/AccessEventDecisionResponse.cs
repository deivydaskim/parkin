using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.Features.AccessEvents;

public record AccessEventDecisionResponse(
  Guid? EventId,
  Decision Decision,
  DenyReason? Reason,
  SessionPool? Pool,
  string? ReservedSpaceLabel,
  Guid? SessionId,
  DateTimeOffset OccurredAt)
{
  public static AccessEventDecisionResponse From(AccessEvent accessEvent, SessionPool? pool = null,
    string? reservedSpaceLabel = null) => new(
    accessEvent.Id.Value,
    accessEvent.Decision,
    accessEvent.DenyReason,
    pool,
    reservedSpaceLabel,
    accessEvent.SessionId?.Value,
    accessEvent.OccurredAt);

  public static AccessEventDecisionResponse LotNotFound(DateTimeOffset occurredAt) => new(
    EventId: null,
    Decision.Deny,
    DenyReason.LotNotFound,
    Pool: null,
    ReservedSpaceLabel: null,
    SessionId: null,
    occurredAt);
}
