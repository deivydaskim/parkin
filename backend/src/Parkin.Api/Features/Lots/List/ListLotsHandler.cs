namespace Parkin.Api.Features.Lots.List;

public record ListLotsQuery(int Page, int PerPage, LotStatusFilter? Status = null, string? Search = null)
  : IQuery<Result<PagedResult<LotResponse>>>;

public class ListLotsHandler(IListLotsQueryService query)
  : IQueryHandler<ListLotsQuery, Result<PagedResult<LotResponse>>>
{
  public async ValueTask<Result<PagedResult<LotResponse>>> Handle(ListLotsQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request, cancellationToken);
}
