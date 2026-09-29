namespace Parkin.Api.Features.Lots.List;

public interface IListLotsQueryService
{
  Task<PagedResult<LotDto>> ListAsync(int page, int perPage, LotStatusFilter? status, string? search = null);
}
