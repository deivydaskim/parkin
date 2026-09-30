using Ardalis.GuardClauses;
using Parkin.Api.Domain.AccessEventAggregate.Events;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;

namespace Parkin.Api.Domain.AccessEventAggregate;

public class AccessEvent : EntityBase<AccessEvent, AccessEventId>, IAggregateRoot
{
  public const string IdempotencyIndexName = "ux_access_event_idempotency";
  public const int IdempotencyKeyMaxLength = 200;

  private AccessEvent() { }

  private AccessEvent(AccessEventId id, ParkingLotId lotId, string rawPlate, string normalizedPlate,
    PlateId? matchedPlateId, DriverId? matchedDriverId, Direction direction, EventSource source,
    Decision decision, DenyReason? denyReason, DateTimeOffset occurredAt, DateTimeOffset receivedAt,
    string idempotencyKey, AccessEventActor actor, AccessEventId? overrideOf)
  {
    Guard.Against.NullOrWhiteSpace(rawPlate, nameof(rawPlate));
    Guard.Against.NullOrWhiteSpace(normalizedPlate, nameof(normalizedPlate));
    Guard.Against.NullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));
    Guard.Against.Null(actor, nameof(actor));

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
    ReceivedAt = receivedAt;
    IdempotencyKey = idempotencyKey;
    ActorId = actor.Id;
    ActingStaffId = actor.StaffId;
    OverrideOf = overrideOf;
  }

  public static AccessEvent Record(ParkingLotId lotId, string rawPlate, string normalizedPlate,
    PlateId? matchedPlateId, DriverId? matchedDriverId, Direction direction, EventSource source,
    Decision decision, DenyReason? denyReason, DateTimeOffset occurredAt, DateTimeOffset receivedAt,
    string idempotencyKey, AccessEventActor actor, AccessEventId? overrideOf = null)
  {
    if (decision == Decision.Allow && denyReason.HasValue)
    {
      throw new ArgumentException("An ALLOW decision cannot carry a deny reason.", nameof(denyReason));
    }

    if (decision == Decision.Deny && !denyReason.HasValue)
    {
      throw new ArgumentException("A DENY decision must carry a reason.", nameof(denyReason));
    }

    var accessEvent = new AccessEvent(AccessEventId.From(Guid.CreateVersion7()), lotId, rawPlate, normalizedPlate,
      matchedPlateId, matchedDriverId, direction, source, decision, denyReason, occurredAt, receivedAt,
      idempotencyKey, actor, overrideOf);
    accessEvent.RegisterDomainEvent(new AccessEventRecordedEvent(accessEvent));
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
  public Guid ActorId { get; private set; }
  public Guid? ActingStaffId { get; private set; }
  public ParkingSessionId? SessionId { get; private set; }
  public AccessEventId? OverrideOf { get; private set; }

  public void AttachSession(ParkingSessionId sessionId) => SessionId = sessionId;
}
