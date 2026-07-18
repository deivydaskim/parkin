namespace Parkin.Api.Domain.AccessGrantAggregate.Events;

public class GrantRevokedEvent(AccessGrantId grantId, Guid? actorId) : DomainEventBase
{
  public AccessGrantId GrantId { get; } = grantId;
  public Guid? ActorId { get; } = actorId;
}
