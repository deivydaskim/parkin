using Ardalis.GuardClauses;
using Parkin.Api.Domain.ApiKeyAggregate.Events;

namespace Parkin.Api.Domain.ApiKeyAggregate;

public class ApiKey : EntityBase<ApiKey, ApiKeyId>, IAggregateRoot
{
  // Private constructor for EF Core
  private ApiKey() { }

  private ApiKey(ApiKeyId id, string name, string keyHash, string prefix, Guid? createdByUserId)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Guard.Against.NullOrWhiteSpace(keyHash, nameof(keyHash));
    Guard.Against.NullOrWhiteSpace(prefix, nameof(prefix));

    Id = id;
    Name = name;
    KeyHash = keyHash;
    Prefix = prefix;
    Status = ApiKeyStatus.Active;
    CreatedAt = DateTimeOffset.UtcNow;
    CreatedByUserId = createdByUserId;
  }

  // Returns the entity alongside the raw secret, which is never persisted and
  // must be surfaced to the caller exactly once.
  public static (ApiKey Entity, string RawKey) Create(string name, Guid? createdByUserId)
  {
    var (rawKey, displayPrefix, hash) = ApiKeySecret.Generate();
    var entity = new ApiKey(ApiKeyId.From(Guid.NewGuid()), name, hash, displayPrefix, createdByUserId);
    entity.RegisterDomainEvent(new ApiKeyCreatedEvent(entity.Id, createdByUserId));
    return (entity, rawKey);
  }

  public string Name { get; private set; } = string.Empty;
  public string KeyHash { get; private set; } = string.Empty;
  public string Prefix { get; private set; } = string.Empty;
  public ApiKeyStatus Status { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }
  public Guid? CreatedByUserId { get; private set; }
  public DateTimeOffset? RevokedAt { get; private set; }
  public Guid? RevokedByUserId { get; private set; }

  public void Revoke(Guid? revokedByUserId)
  {
    if (Status == ApiKeyStatus.Revoked) return;

    Status = ApiKeyStatus.Revoked;
    RevokedAt = DateTimeOffset.UtcNow;
    RevokedByUserId = revokedByUserId;
    RegisterDomainEvent(new ApiKeyRevokedEvent(Id, revokedByUserId));
  }
}
