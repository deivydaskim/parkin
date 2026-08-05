using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.Services;

// Materialized, I/O-free snapshot of everything EntryDecisionService needs to decide an
// ENTER attempt for one plate at one lot, at one instant. The future T5.2 handler resolves
// plate -> driver/grant/reservation/active-GENERAL-count from the DB and builds this via Create.
//
// HasActiveGrant / HasActiveReservation are already filtered to "currently active": an expired
// or revoked grant, and a cancelled reservation, both collapse to false here - the boolean makes
// no distinction between "never existed" and "no longer active", by design (see T5.1 notes).
public sealed record EntryDecisionContext
{
  public bool IsPlateKnown { get; private init; }
  public AccessMode LotAccessMode { get; private init; }
  public FullBehavior LotFullBehavior { get; private init; }
  public bool HasActiveGrant { get; private init; }
  public bool HasActiveReservation { get; private init; }
  public string? ReservedSpaceLabel { get; private init; }
  public bool IsGeneralPoolFull { get; private init; }

  public static EntryDecisionContext Create(
    bool isPlateKnown,
    AccessMode lotAccessMode,
    FullBehavior lotFullBehavior,
    bool hasActiveGrant,
    bool hasActiveReservation,
    bool isGeneralPoolFull,
    string? reservedSpaceLabel = null)
  {
    if (!isPlateKnown && hasActiveGrant)
    {
      throw new ArgumentException("An unknown plate cannot hold an active grant.", nameof(hasActiveGrant));
    }

    if (!isPlateKnown && hasActiveReservation)
    {
      throw new ArgumentException("An unknown plate cannot hold an active reservation.", nameof(hasActiveReservation));
    }

    if (hasActiveReservation && string.IsNullOrWhiteSpace(reservedSpaceLabel))
    {
      throw new ArgumentException("An active reservation must carry its assigned space label.", nameof(reservedSpaceLabel));
    }

    if (!hasActiveReservation && reservedSpaceLabel is not null)
    {
      throw new ArgumentException("A reserved space label requires an active reservation.", nameof(reservedSpaceLabel));
    }

    return new EntryDecisionContext
    {
      IsPlateKnown = isPlateKnown,
      LotAccessMode = lotAccessMode,
      LotFullBehavior = lotFullBehavior,
      HasActiveGrant = hasActiveGrant,
      HasActiveReservation = hasActiveReservation,
      ReservedSpaceLabel = reservedSpaceLabel,
      IsGeneralPoolFull = isGeneralPoolFull
    };
  }
}
