using Ardalis.Result;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Domain.ApiKeyAggregate.Events;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.Domain.ApiKeyAggregate;

public class ApiKeyTests
{
  private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
  private static readonly DateTimeOffset RevokedAt = CreatedAt.AddDays(3);

  [Fact]
  public void Create_UsesSuppliedTimestamp_AndHashesTheRawKey()
  {
    var actorId = Guid.NewGuid();

    var (apiKey, rawKey) = ApiKey.Create("Gate 1", actorId, CreatedAt);

    apiKey.CreatedAt.ShouldBe(CreatedAt);
    apiKey.CreatedByUserId.ShouldBe(actorId);
    apiKey.Status.ShouldBe(ApiKeyStatus.Active);
    apiKey.KeyHash.ShouldBe(ApiKeySecret.Hash(rawKey));
    rawKey.ShouldStartWith(apiKey.Prefix);
    apiKey.DomainEvents.ShouldContain(e => e is ApiKeyCreatedEvent);
  }

  [Fact]
  public void Revoke_ActiveKey_RecordsTimestampAndActor()
  {
    var (apiKey, _) = ApiKey.Create("Gate 1", null, CreatedAt);
    var actorId = Guid.NewGuid();

    var result = apiKey.Revoke(actorId, RevokedAt);

    result.IsSuccess.ShouldBeTrue();
    apiKey.Status.ShouldBe(ApiKeyStatus.Revoked);
    apiKey.RevokedAt.ShouldBe(RevokedAt);
    apiKey.RevokedByUserId.ShouldBe(actorId);
    apiKey.DomainEvents.ShouldContain(e => e is ApiKeyRevokedEvent);
  }

  [Fact]
  public void Revoke_AlreadyRevokedKey_ReturnsConflict_AndKeepsOriginalRevocation()
  {
    var (apiKey, _) = ApiKey.Create("Gate 1", null, CreatedAt);
    apiKey.Revoke(null, RevokedAt);

    var result = apiKey.Revoke(Guid.NewGuid(), RevokedAt.AddHours(1));

    result.Status.ShouldBe(ResultStatus.Conflict);
    apiKey.RevokedAt.ShouldBe(RevokedAt);
    apiKey.RevokedByUserId.ShouldBeNull();
    apiKey.DomainEvents.Count(e => e is ApiKeyRevokedEvent).ShouldBe(1);
  }
}
