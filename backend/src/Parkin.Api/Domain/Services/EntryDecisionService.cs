using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.Services;

// The gate's single source of truth (architecture doc §8). Pure, deterministic, no I/O.
// Implements the rule-4 precedence exactly, in the order the architecture flowchart evaluates it:
//   1. Unknown plate + RESTRICTED lot            -> DENY NOT_AUTHORIZED
//   2. Known plate, RESTRICTED lot, no grant      -> DENY NOT_AUTHORIZED   (reservation already excluded by rule 3 below)
//   3. Active reservation                          -> ALLOW RESERVED, bypassing full/overflow entirely
//   4. General entrant, lot full, BLOCK            -> DENY LOT_FULL
//   5. Otherwise (not full, or full + ALLOW_OVERFLOW) -> ALLOW GENERAL (flagged over-capacity in the overflow case)
//
// Rule 3 is checked before rule 2 so a reservation always wins even on a RESTRICTED lot without a
// grant (T4.1: "reserved driver implicitly has lot access even if RESTRICTED"). A grant alone does
// NOT bypass full/overflow - a grant holder without a reservation is just a general entrant once
// past the RESTRICTED gate, subject to the same full-lot rules as anyone else.
public sealed class EntryDecisionService : IEntryDecisionService
{
  public EntryDecision Decide(EntryDecisionContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var isRestricted = context.LotAccessMode == AccessMode.Restricted;

    // Rule 1: unknown plate on a RESTRICTED lot. (An unknown plate can never carry a grant or
    // reservation - guarded by EntryDecisionContext.Create - so this is unconditional.)
    if (!context.IsPlateKnown && isRestricted)
    {
      return EntryDecision.Deny(DecisionReason.NotAuthorized);
    }

    // Rule 3: a reserved holder is always let in, on any lot mode, bypassing full/overflow.
    if (context.HasActiveReservation)
    {
      return EntryDecision.Allow(SessionPool.Reserved, context.ReservedSpaceLabel);
    }

    // Rule 2: known plate, RESTRICTED lot, no active grant (and, by this point, no reservation).
    if (isRestricted && !context.HasActiveGrant)
    {
      return EntryDecision.Deny(DecisionReason.NotAuthorized);
    }

    // General entrant: OPEN lot (grant/reservation irrelevant), or RESTRICTED lot with a grant.
    if (!context.IsGeneralPoolFull)
    {
      return EntryDecision.Allow(SessionPool.General);
    }

    // Rule 4: general pool is full and the lot blocks overflow.
    if (context.LotFullBehavior == FullBehavior.Block)
    {
      return EntryDecision.Deny(DecisionReason.LotFull);
    }

    // Rule 5: full, but the lot allows overflow - let them in, flagged for occupancy reporting.
    return EntryDecision.Allow(SessionPool.General, isOverCapacity: true);
  }
}
