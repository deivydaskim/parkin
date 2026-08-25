using Ardalis.GuardClauses;
using Parkin.Api.Domain.AccessEventAggregate.Events;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;

namespace Parkin.Api.Domain.AccessEventAggregate;

public class AccessEvent : EntityBase<AccessEvent, AccessEventId>, IAggregateRoot
{
  // Private constructor for EF Core
  private AccessEvent() { }

  private AccessEvent(AccessEventId id, ParkingLotId lotId, string rawPlate, string normalizedPlate,
    PlateId? matchedPlateId, DriverId? matchedDriverId, Direction direction, EventSource source,
    Decision decision, DenyReason? denyReason, DateTimeOffset occurredAt, string idempotencyKey,
    Guid? actingStaffId, AccessEventId? overrideOf)
  {
    Guard.Against.NullOrWhiteSpace(rawPlate, nameof(rawPlate));
    Guard.Against.NullOrWhiteSpace(normalizedPlate, nameof(normalizedPlate));
    Guard.Against.NullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));

    Id = id;
    LotId = lotId;
    RawPlate = rawPlate;
    NormalizedPlate = normalizedPlate;
    MatchedPlateId = matchedPlateId;
    MatchedDriverId = matchedDriverId;
    Direction = direction;
    Source = source;
    Decision = decision;
    DenyReason = denyReason;
    OccurredAt = occurredAt;
    ReceivedAt = DateTimeOffset.UtcNow;
    IdempotencyKey = idempotencyKey;
    ActingStaffId = actingStaffId;
    OverrideOf = overrideOf;
  }

  public static AccessEvent Record(ParkingLotId lotId, string rawPlate, string normalizedPlate,
    PlateId? matchedPlateId, DriverId? matchedDriverId, Direction direction, EventSource source,
    Decision decision, DenyReason? denyReason, DateTimeOffset occurredAt, string idempotencyKey,
    Guid? actorId, Guid? actingStaffId = null, AccessEventId? overrideOf = null)
  {
    if (decision == Decision.Allow && denyReason.HasValue)
    {
      throw new ArgumentException("An ALLOW decision cannot carry a deny reason.", nameof(denyReason));
    }

    if (decision == Decision.Deny && !denyReason.HasValue)
    {
      throw new ArgumentException("A DENY decision must carry a reason.", nameof(denyReason));
    }

    var accessEvent = new AccessEvent(AccessEventId.From(Guid.NewGuid()), lotId, rawPlate, normalizedPlate,
      matchedPlateId, matchedDriverId, direction, source, decision, denyReason, occurredAt, idempotencyKey,
      actingStaffId, overrideOf);
    accessEvent.RegisterDomainEvent(new AccessEventRecordedEvent(accessEvent, actorId));
    return accessEvent;
  }

  public ParkingLotId LotId { get; private set; }
  public string RawPlate { get; private set; } = string.Empty;
  public string NormalizedPlate { get; private set; } = string.Empty;
  public PlateId? MatchedPlateId { get; private set; }
  public DriverId? MatchedDriverId { get; private set; }
  public Direction Direction { get; private set; }
  public EventSource Source { get; private set; }
  public Decision Decision { get; private set; }
  public DenyReason? DenyReason { get; private set; }
  public DateTimeOffset OccurredAt { get; private set; }
  public DateTimeOffset ReceivedAt { get; private set; }
  public string IdempotencyKey { get; private set; } = string.Empty;
  public Guid? ActingStaffId { get; private set; }
  public ParkingSessionId? SessionId { get; private set; }
  public AccessEventId? OverrideOf { get; private set; }

  // The session and its entry event reference each other, so one link is filled in after both
  // objects exist. Ids are generated in memory, so this only ever touches an unpersisted event.
  public void AttachSession(ParkingSessionId sessionId) => SessionId = sessionId;
}
