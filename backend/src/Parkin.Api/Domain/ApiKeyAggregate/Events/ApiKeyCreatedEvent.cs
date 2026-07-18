namespace Parkin.Api.Domain.ApiKeyAggregate.Events;

public class ApiKeyCreatedEvent(ApiKeyId apiKeyId, Guid? actorId) : DomainEventBase
{
  public ApiKeyId ApiKeyId { get; } = apiKeyId;
  public Guid? ActorId { get; } = actorId;
}
