namespace Parkin.Api.ApiKeyFeatures.List;

public interface IListApiKeysQueryService
{
  Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken);
}
