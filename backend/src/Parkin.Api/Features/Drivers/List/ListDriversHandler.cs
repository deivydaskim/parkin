namespace Parkin.Api.Features.Drivers.List;

public record ListDriversQuery(int Page, int PerPage, DriverStatusFilter? Status, string? Search)
  : IQuery<Result<PagedResult<DriverResponse>>>;

public class ListDriversHandler(IListDriversQueryService query)
  : IQueryHandler<ListDriversQuery, Result<PagedResult<DriverResponse>>>
{
  public async ValueTask<Result<PagedResult<DriverResponse>>> Handle(ListDriversQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request.Page, request.PerPage, request.Status, request.Search, cancellationToken);
}
