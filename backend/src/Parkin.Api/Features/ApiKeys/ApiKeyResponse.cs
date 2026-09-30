using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.Features.ApiKeys;

public record ApiKeyResponse(
  Guid Id,
  string Name,
  Guid LotId,
  string LotName,
  string Prefix,
  ApiKeyStatus Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? RevokedAt)
{
  public static ApiKeyResponse From(ApiKey apiKey, string lotName)
    => new(apiKey.Id.Value, apiKey.Name, apiKey.LotId.Value, lotName, apiKey.Prefix, apiKey.Status, apiKey.CreatedAt,
      apiKey.RevokedAt);
}
