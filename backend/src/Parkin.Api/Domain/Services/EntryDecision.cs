namespace Parkin.Api.Domain.Services;

// Result of EntryDecisionService.Decide. Reason/Pool/ReservedSpaceLabel are mutually exclusive
// with each other depending on Outcome (mirrors the nullable "reason"/"pool" fields in the
// POST /api/v1/access-events response shape from the architecture doc).
public sealed record EntryDecision
{
  public required EntryDecisionOutcome Outcome { get; init; }
  public DecisionReason? Reason { get; init; }
  public SessionPool? Pool { get; init; }
  public string? ReservedSpaceLabel { get; init; }
  public bool IsOverCapacity { get; init; }

  public static EntryDecision Allow(SessionPool pool, string? reservedSpaceLabel = null, bool isOverCapacity = false) => new()
  {
    Outcome = EntryDecisionOutcome.Allow,
    Pool = pool,
    ReservedSpaceLabel = reservedSpaceLabel,
    IsOverCapacity = isOverCapacity
  };

  public static EntryDecision Deny(DecisionReason reason) => new()
  {
    Outcome = EntryDecisionOutcome.Deny,
    Reason = reason
  };
}
