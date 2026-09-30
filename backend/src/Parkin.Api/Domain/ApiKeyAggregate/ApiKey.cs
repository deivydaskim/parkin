using Ardalis.GuardClauses;
using Parkin.Api.Domain.ApiKeyAggregate.Events;

namespace Parkin.Api.Domain.ApiKeyAggregate;

public class ApiKey : EntityBase<ApiKey, ApiKeyId>, IAggregateRoot
{
  private ApiKey() { }

  private ApiKey(ApiKeyId id, string name, string keyHash, string prefix, Guid? createdByUserId,
    DateTimeOffset createdAt)
  {
    Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Guard.Against.NullOrWhiteSpace(keyHash, nameof(keyHash));
    Guard.Against.NullOrWhiteSpace(prefix, nameof(prefix));

    Id = id;
    Name = name;
    KeyHash = keyHash;
    Prefix = prefix;
    Status = ApiKeyStatus.Active;
    CreatedAt = createdAt;
    CreatedByUserId = createdByUserId;
  }

  public static (ApiKey Entity, string RawKey) Create(string name, Guid? createdByUserId, DateTimeOffset now)
  {
    var (rawKey, displayPrefix, hash) = ApiKeySecret.Generate();
    var entity = new ApiKey(ApiKeyId.From(Guid.CreateVersion7()), name, hash, displayPrefix, createdByUserId, now);
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

  public Result Revoke(Guid? revokedByUserId, DateTimeOffset now)
  {
    if (Status == ApiKeyStatus.Revoked) return Result.Conflict("API key is already revoked.");

    Status = ApiKeyStatus.Revoked;
    RevokedAt = now;
    RevokedByUserId = revokedByUserId;
    RegisterDomainEvent(new ApiKeyRevokedEvent(Id, revokedByUserId));
    return Result.Success();
  }
}
