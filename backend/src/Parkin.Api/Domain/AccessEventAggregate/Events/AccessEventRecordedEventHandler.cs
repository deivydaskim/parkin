using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.AccessEventAggregate.Events;

public class AccessEventRecordedEventHandler(IRepository<AuditLogEntry> auditRepository)
  : INotificationHandler<AccessEventRecordedEvent>
{
  public async ValueTask Handle(AccessEventRecordedEvent notification, CancellationToken cancellationToken)
  {
    var accessEvent = notification.AccessEvent;

    var entry = AuditLogEntry.Create(
      AuditActorType.Api,
      notification.ActorId,
      AuditActions.AccessEventIngested,
      AuditEntityTypes.AccessEvent,
      accessEvent.Id.Value,
      new
      {
        lotId = accessEvent.LotId.Value,
        plate = accessEvent.NormalizedPlate,
        direction = accessEvent.Direction.ToString(),
        source = accessEvent.Source.ToString(),
        decision = accessEvent.Decision.ToString(),
        reason = accessEvent.DenyReason?.ToString(),
        driverId = accessEvent.MatchedDriverId?.Value,
        sessionId = accessEvent.SessionId?.Value
      });

    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);
  }
}
