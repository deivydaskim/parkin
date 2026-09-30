namespace Parkin.Api.Domain.AuditAggregate;

public abstract class AuditableDomainEvent(Guid? actorId, string action, string entityType, Guid entityId)
  : DomainEventBase
{
  public Guid? ActorId { get; } = actorId;
  public string Action { get; } = action;
  public string EntityType { get; } = entityType;
  public Guid EntityId { get; } = entityId;

  public virtual AuditActorType ActorType => ActorId.HasValue ? AuditActorType.Staff : AuditActorType.System;

  public virtual object? Metadata => null;
}
