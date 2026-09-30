using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.Services;

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

  public static EntryDecision Decide(EntryDecisionContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var isRestricted = context.LotAccessMode == AccessMode.Restricted;

    if (!context.IsPlateKnown && isRestricted)
    {
      return Deny(DecisionReason.NotAuthorized);
    }

    // A reservation is checked before the grant so a reserved driver gets in even on a RESTRICTED
    // lot without a grant, and bypasses the full-lot rules entirely.
    if (context.HasActiveReservation)
    {
      return Allow(SessionPool.Reserved, context.ReservedSpaceLabel);
    }

    if (isRestricted && !context.HasActiveGrant)
    {
      return Deny(DecisionReason.NotAuthorized);
    }

    if (!context.IsGeneralPoolFull)
    {
      return Allow(SessionPool.General);
    }

    return context.LotFullBehavior == FullBehavior.Block
      ? Deny(DecisionReason.LotFull)
      : Allow(SessionPool.General, isOverCapacity: true);
  }
}
