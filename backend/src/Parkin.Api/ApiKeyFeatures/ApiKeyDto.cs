using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.ApiKeyFeatures;

public record ApiKeyDto(
  ApiKeyId Id,
  string Name,
  string Prefix,
  ApiKeyStatus Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? RevokedAt);
