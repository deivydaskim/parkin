namespace Parkin.Api.ApiKeyFeatures;

internal static class ApiKeyMapping
{
  public static ApiKeyRecord ToRecord(ApiKeyDto dto) => new(
    dto.Id.Value,
    dto.Name,
    dto.Prefix,
    dto.Status,
    dto.CreatedAt,
    dto.RevokedAt);
}
