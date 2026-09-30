using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ApiKeyAggregate.Events;

public class ApiKeyRevokedEvent(ApiKeyId apiKeyId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.ApiKeyRevoke, AuditEntityTypes.ApiKey, apiKeyId.Value)
{
  public ApiKeyId ApiKeyId { get; } = apiKeyId;
}
