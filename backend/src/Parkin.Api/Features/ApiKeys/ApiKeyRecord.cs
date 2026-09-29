using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.Features.ApiKeys;

public record ApiKeyRecord(
  Guid Id,
  string Name,
  string Prefix,
  ApiKeyStatus Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? RevokedAt);
