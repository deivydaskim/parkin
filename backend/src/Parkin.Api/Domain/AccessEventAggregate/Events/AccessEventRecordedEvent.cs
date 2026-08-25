namespace Parkin.Api.Domain.AccessEventAggregate.Events;

// Carries the entity, not ids: the handler runs after SaveChanges, so AttachSession has already
// run and the session link is available to audit.
public class AccessEventRecordedEvent(AccessEvent accessEvent, Guid? actorId) : DomainEventBase
{
  public AccessEvent AccessEvent { get; } = accessEvent;
  public Guid? ActorId { get; } = actorId;
}
