namespace Parkin.Api.LotFeatures.List;

/// <summary>
/// Represents a service that will actually fetch the necessary data
/// Typically implemented in Infrastructure
/// </summary>
public interface IListLotsQueryService
{
  Task<PagedResult<LotDto>> ListAsync(int page, int perPage, string? status);
}
