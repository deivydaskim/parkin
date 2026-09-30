namespace Parkin.Api.Features.Lots.List;

public interface IListLotsQueryService
{
  Task<PagedResult<LotResponse>> ListAsync(ListLotsQuery query, CancellationToken cancellationToken);
}
