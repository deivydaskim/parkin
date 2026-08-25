namespace Parkin.Api.AccessEventFeatures;

internal static class AccessEventMapping
{
  public static AccessEventDecisionRecord ToRecord(AccessEventDecisionDto dto) => new(
    dto.EventId?.Value,
    dto.Decision,
    dto.Reason,
    dto.Pool,
    dto.ReservedSpaceLabel,
    dto.SessionId?.Value,
    dto.OccurredAt);
}
