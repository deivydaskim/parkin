namespace Parkin.Api.Domain.Services;

// Only meaningful when EntryDecision.Outcome is Deny; null on Allow.
public enum DecisionReason
{
  NotAuthorized,
  LotFull
}
