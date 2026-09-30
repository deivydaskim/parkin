using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.ApiKeyAggregate.Events;

public class ApiKeyCreatedEvent(ApiKeyId apiKeyId, Guid? actorId)
  : AuditableDomainEvent(actorId, AuditActions.ApiKeyCreate, AuditEntityTypes.ApiKey, apiKeyId.Value)
{
  public ApiKeyId ApiKeyId { get; } = apiKeyId;
}
