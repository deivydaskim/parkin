using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.AccessGrantAggregate.Events;

public class GrantRevokedEvent(AccessGrantId grantId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.GrantRevoked, AuditEntityTypes.AccessGrant, grantId.Value)
{
  public AccessGrantId GrantId { get; } = grantId;
}
