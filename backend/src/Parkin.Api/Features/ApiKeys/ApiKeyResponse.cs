using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.Features.ApiKeys;

public record ApiKeyResponse(
  Guid Id,
  string Name,
  string Prefix,
  ApiKeyStatus Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? RevokedAt)
{
  public static ApiKeyResponse From(ApiKey apiKey)
    => new(apiKey.Id.Value, apiKey.Name, apiKey.Prefix, apiKey.Status, apiKey.CreatedAt, apiKey.RevokedAt);
}
