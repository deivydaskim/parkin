namespace Parkin.Api.LotFeatures.List;

public interface IListLotsQueryService
{
  Task<PagedResult<LotDto>> ListAsync(int page, int perPage, LotStatusFilter? status);
}
