namespace Parkin.Api.Domain.ApiKeyAggregate.Specifications;

public class ApiKeyByIdSpec : Specification<ApiKey>
{
  public ApiKeyByIdSpec(ApiKeyId apiKeyId) =>
    Query
        .Where(apiKey => apiKey.Id == apiKeyId);
}
