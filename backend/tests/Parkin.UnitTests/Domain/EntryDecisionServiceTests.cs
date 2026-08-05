using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.Services;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain;

public class EntryDecisionServiceTests
{
  private readonly EntryDecisionService _sut = new();

  private const string SpaceLabel = "A-12";

  // ---------------------------------------------------------------------------
  // Rule 1: unknown plate + RESTRICTED lot -> DENY NOT_AUTHORIZED, unconditionally
  // (full state and full behavior must not matter - unauthorized short-circuits first).
  // ---------------------------------------------------------------------------

  [Theory]
  [InlineData(false, FullBehavior.Block)]
  [InlineData(false, FullBehavior.AllowOverflow)]
  [InlineData(true, FullBehavior.Block)]
  [InlineData(true, FullBehavior.AllowOverflow)]
  public void UnknownPlate_OnRestrictedLot_IsAlwaysDeniedNotAuthorized(bool isGeneralPoolFull, FullBehavior fullBehavior)
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: false,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: fullBehavior,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: isGeneralPoolFull);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Deny);
    decision.Reason.ShouldBe(DecisionReason.NotAuthorized);
    decision.Pool.ShouldBeNull();
  }

  // ---------------------------------------------------------------------------
  // Rule 1 boundary: unknown plate on an OPEN lot is NOT unauthorized - it's a general
  // entrant, subject to the same full/overflow rules as anyone else. Pins the ambiguous
  // "what happens on an OPEN, full, BLOCK lot for an unknown plate" case explicitly:
  // it is LOT_FULL, never NOT_AUTHORIZED.
  // ---------------------------------------------------------------------------

  [Fact]
  public void UnknownPlate_OnOpenLot_NotFull_IsAllowedGeneral()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: false,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: false);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.General);
    decision.Reason.ShouldBeNull();
    decision.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public void UnknownPlate_OnOpenFullLotWithBlockBehavior_IsDeniedLotFull_NotNotAuthorized()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: false,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: true);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Deny);
    decision.Reason.ShouldBe(DecisionReason.LotFull);
  }

  [Fact]
  public void UnknownPlate_OnOpenFullLotWithAllowOverflowBehavior_IsAllowedGeneral_FlaggedOverCapacity()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: false,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.AllowOverflow,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: true);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.General);
    decision.IsOverCapacity.ShouldBeTrue();
  }

  // ---------------------------------------------------------------------------
  // Rule 2: known plate, RESTRICTED lot, no active grant, no active reservation -> DENY.
  // Full state must not matter - unauthorized short-circuits before the full check.
  // ---------------------------------------------------------------------------

  [Theory]
  [InlineData(false, FullBehavior.Block)]
  [InlineData(false, FullBehavior.AllowOverflow)]
  [InlineData(true, FullBehavior.Block)]
  [InlineData(true, FullBehavior.AllowOverflow)]
  public void KnownPlateWithNoGrantAndNoReservation_OnRestrictedLot_IsAlwaysDeniedNotAuthorized(bool isGeneralPoolFull, FullBehavior fullBehavior)
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: fullBehavior,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: isGeneralPoolFull);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Deny);
    decision.Reason.ShouldBe(DecisionReason.NotAuthorized);
  }

  [Fact]
  public void KnownPlateWithNoActiveGrant_OnRestrictedLot_IsDenied_SameAsExpiredOrRevokedGrant()
  {
    // HasActiveGrant=false is the materialized representation of "no currently-active grant" -
    // it makes no distinction between never-granted, expired, or revoked. T5.2's handler is
    // responsible for collapsing all three into this single boolean before calling Decide().
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: false);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Deny);
    decision.Reason.ShouldBe(DecisionReason.NotAuthorized);
  }

  // ---------------------------------------------------------------------------
  // Rule 3: an active reservation ALWAYS allows entry as RESERVED, bypassing full/overflow
  // entirely - on any lot mode, any full state, any full behavior, with or without a grant.
  // ---------------------------------------------------------------------------

  [Theory]
  [InlineData(AccessMode.Restricted, FullBehavior.Block, true, false)]
  [InlineData(AccessMode.Restricted, FullBehavior.Block, false, false)]
  [InlineData(AccessMode.Restricted, FullBehavior.AllowOverflow, true, true)]
  [InlineData(AccessMode.Restricted, FullBehavior.AllowOverflow, false, true)]
  [InlineData(AccessMode.Open, FullBehavior.Block, true, false)]
  [InlineData(AccessMode.Open, FullBehavior.Block, false, false)]
  [InlineData(AccessMode.Open, FullBehavior.AllowOverflow, true, true)]
  [InlineData(AccessMode.Open, FullBehavior.AllowOverflow, false, true)]
  public void ReservedHolder_AlwaysAllowedReserved_BypassingFullOverflow_RegardlessOfGrant(
    AccessMode lotAccessMode, FullBehavior fullBehavior, bool isGeneralPoolFull, bool hasActiveGrant)
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: lotAccessMode,
      lotFullBehavior: fullBehavior,
      hasActiveGrant: hasActiveGrant,
      hasActiveReservation: true,
      isGeneralPoolFull: isGeneralPoolFull,
      reservedSpaceLabel: SpaceLabel);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.Reserved);
    decision.ReservedSpaceLabel.ShouldBe(SpaceLabel);
    decision.Reason.ShouldBeNull();
    // Reserved entries never carry the general over-capacity flag - they were never subject
    // to the general-pool full check at all.
    decision.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public void ReservedHolder_OnRestrictedLotWithoutGrant_IsAllowed_BecauseReservationImpliesAccess()
  {
    // T4.1: "reserved driver implicitly has lot access even if RESTRICTED" - a reservation
    // alone is sufficient; no grant is required.
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: true,
      isGeneralPoolFull: false,
      reservedSpaceLabel: SpaceLabel);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.Reserved);
  }

  // ---------------------------------------------------------------------------
  // Grant holder (no reservation): a grant only clears the RESTRICTED gate - it does NOT
  // bypass full/overflow. Once past RESTRICTED, a grant holder is an ordinary general
  // entrant. This is the explicit resolution of the "does a grant bypass full like a
  // reservation does" ambiguity: it does not.
  // ---------------------------------------------------------------------------

  [Fact]
  public void GrantHolder_OnRestrictedLot_NotFull_IsAllowedGeneral()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: true,
      hasActiveReservation: false,
      isGeneralPoolFull: false);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.General);
  }

  [Fact]
  public void GrantHolder_OnRestrictedFullLotWithBlockBehavior_IsDeniedLotFull_GrantDoesNotBypassFull()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: true,
      hasActiveReservation: false,
      isGeneralPoolFull: true);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Deny);
    decision.Reason.ShouldBe(DecisionReason.LotFull);
  }

  [Fact]
  public void GrantHolder_OnRestrictedFullLotWithAllowOverflowBehavior_IsAllowedGeneral_FlaggedOverCapacity()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.AllowOverflow,
      hasActiveGrant: true,
      hasActiveReservation: false,
      isGeneralPoolFull: true);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.General);
    decision.IsOverCapacity.ShouldBeTrue();
  }

  [Fact]
  public void GrantHolder_OnOpenLot_GrantIsIrrelevant_TreatedAsOrdinaryGeneralEntrant()
  {
    // C2 AC: "grant on an OPEN lot changes nothing" - same outcome with or without the grant.
    var withGrant = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: true,
      hasActiveReservation: false,
      isGeneralPoolFull: false);

    var withoutGrant = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: false);

    var decisionWithGrant = _sut.Decide(withGrant);
    var decisionWithoutGrant = _sut.Decide(withoutGrant);

    decisionWithGrant.ShouldBe(decisionWithoutGrant);
    decisionWithGrant.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decisionWithGrant.Pool.ShouldBe(SessionPool.General);
  }

  // ---------------------------------------------------------------------------
  // Rule 4/5: general entrant, full-lot behavior matrix (OPEN lot, known plate,
  // no grant/reservation needed since OPEN never requires either).
  // ---------------------------------------------------------------------------

  [Fact]
  public void GeneralEntrant_NotFull_IsAllowedGeneral_NotFlaggedOverCapacity()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: false);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.General);
    decision.IsOverCapacity.ShouldBeFalse();
  }

  [Fact]
  public void GeneralEntrant_Full_WithBlockBehavior_IsDeniedLotFull()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: true);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Deny);
    decision.Reason.ShouldBe(DecisionReason.LotFull);
    decision.Pool.ShouldBeNull();
  }

  [Fact]
  public void GeneralEntrant_Full_WithAllowOverflowBehavior_IsAllowedGeneral_FlaggedOverCapacity()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.AllowOverflow,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: true);

    var decision = _sut.Decide(context);

    decision.Outcome.ShouldBe(EntryDecisionOutcome.Allow);
    decision.Pool.ShouldBe(SessionPool.General);
    decision.IsOverCapacity.ShouldBeTrue();
  }

  [Fact]
  public void Decide_ThrowsOnNullContext()
  {
    Should.Throw<ArgumentNullException>(() => _sut.Decide(null!));
  }
}

public class EntryDecisionContextTests
{
  // ---------------------------------------------------------------------------
  // EntryDecisionContext.Create guards against domain states that can never legitimately
  // occur (an unknown plate is never linked to a driver, so it can never carry a grant or
  // reservation). These guard the invariant at the boundary, closest to where a future T5.2
  // handler would construct the context from real data.
  // ---------------------------------------------------------------------------

  [Fact]
  public void Create_UnknownPlateWithActiveGrant_Throws()
  {
    Should.Throw<ArgumentException>(() => EntryDecisionContext.Create(
      isPlateKnown: false,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: true,
      hasActiveReservation: false,
      isGeneralPoolFull: false));
  }

  [Fact]
  public void Create_UnknownPlateWithActiveReservation_Throws()
  {
    Should.Throw<ArgumentException>(() => EntryDecisionContext.Create(
      isPlateKnown: false,
      lotAccessMode: AccessMode.Open,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: true,
      isGeneralPoolFull: false,
      reservedSpaceLabel: "A-1"));
  }

  [Fact]
  public void Create_ActiveReservationWithoutSpaceLabel_Throws()
  {
    Should.Throw<ArgumentException>(() => EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: true,
      isGeneralPoolFull: false,
      reservedSpaceLabel: null));
  }

  [Fact]
  public void Create_ActiveReservationWithBlankSpaceLabel_Throws()
  {
    Should.Throw<ArgumentException>(() => EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: true,
      isGeneralPoolFull: false,
      reservedSpaceLabel: "   "));
  }

  [Fact]
  public void Create_SpaceLabelWithoutActiveReservation_Throws()
  {
    Should.Throw<ArgumentException>(() => EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: false,
      isGeneralPoolFull: false,
      reservedSpaceLabel: "A-1"));
  }

  [Fact]
  public void Create_ValidReservedContext_Succeeds()
  {
    var context = EntryDecisionContext.Create(
      isPlateKnown: true,
      lotAccessMode: AccessMode.Restricted,
      lotFullBehavior: FullBehavior.Block,
      hasActiveGrant: false,
      hasActiveReservation: true,
      isGeneralPoolFull: false,
      reservedSpaceLabel: "A-1");

    context.HasActiveReservation.ShouldBeTrue();
    context.ReservedSpaceLabel.ShouldBe("A-1");
  }
}
