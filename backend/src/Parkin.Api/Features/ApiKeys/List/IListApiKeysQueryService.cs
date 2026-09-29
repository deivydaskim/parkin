namespace Parkin.Api.Features.ApiKeys.List;

public interface IListApiKeysQueryService
{
  Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken);
}
