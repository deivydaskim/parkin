namespace Parkin.Api.DriverFeatures.List;

public record ListDriversQuery(int? Page = 1, int? PerPage = Constants.DEFAULT_PAGE_SIZE,
  DriverStatusFilter? Status = null)
  : IQuery<Result<PagedResult<DriverDto>>>;

public class ListDriversHandler(IListDriversQueryService query)
  : IQueryHandler<ListDriversQuery, Result<PagedResult<DriverDto>>>
{
  private readonly IListDriversQueryService _query = query;

  public async ValueTask<Result<PagedResult<DriverDto>>> Handle(ListDriversQuery request,
                                                                  CancellationToken cancellationToken)
  {
    var result = await _query.ListAsync(request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE, request.Status);

    return Result.Success(result);
  }
}
