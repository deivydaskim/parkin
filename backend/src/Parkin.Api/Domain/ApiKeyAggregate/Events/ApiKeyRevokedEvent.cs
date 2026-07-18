namespace Parkin.Api.Domain.ApiKeyAggregate.Events;

public class ApiKeyRevokedEvent(ApiKeyId apiKeyId, Guid? actorId) : DomainEventBase
{
  public ApiKeyId ApiKeyId { get; } = apiKeyId;
  public Guid? ActorId { get; } = actorId;
}
