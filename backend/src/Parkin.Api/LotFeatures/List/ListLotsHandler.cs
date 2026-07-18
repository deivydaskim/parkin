namespace Parkin.Api.LotFeatures.List;

public record ListLotsQuery(int? Page = 1,
  int? PerPage = Constants.DEFAULT_PAGE_SIZE,
  LotStatusFilter? Status = null)
  : IQuery<Result<PagedResult<LotDto>>>;

public class ListLotsHandler(IListLotsQueryService query) : IQueryHandler<ListLotsQuery, Result<PagedResult<LotDto>>>
{
  private readonly IListLotsQueryService _query = query;

  public async ValueTask<Result<PagedResult<LotDto>>> Handle(ListLotsQuery request,
                                                               CancellationToken cancellationToken)
  {
    var result = await _query.ListAsync(request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE, request.Status);

    return Result.Success(result);
  }
}
