using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.AccessEventAggregate.Events;

public class AccessEventRecordedEvent(AccessEvent accessEvent)
  : AuditableDomainEvent(accessEvent.ActorId, AuditActions.AccessEventIngested,
    AuditEntityTypes.AccessEvent, accessEvent.Id.Value)
{
  public AccessEvent AccessEvent { get; } = accessEvent;

  public override AuditActorType ActorType =>
    AccessEvent.ActingStaffId is null ? AuditActorType.Api : AuditActorType.Staff;

  public override object Metadata => new
  {
    lotId = AccessEvent.LotId.Value,
    plate = AccessEvent.NormalizedPlate,
    direction = AccessEvent.Direction.ToString(),
    source = AccessEvent.Source.ToString(),
    decision = AccessEvent.Decision.ToString(),
    reason = AccessEvent.DenyReason?.ToString(),
    driverId = AccessEvent.MatchedDriverId?.Value,
    sessionId = AccessEvent.SessionId?.Value,
    actingStaffId = AccessEvent.ActingStaffId
  };
}
