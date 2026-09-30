namespace Parkin.Api.Features.ApiKeys.List;

public interface IListApiKeysQueryService
{
  Task<IReadOnlyList<ApiKeyResponse>> ListAsync(CancellationToken cancellationToken);
}
