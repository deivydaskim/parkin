using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.GrantFeatures.List;

public record ListGrantsByDriverQuery(DriverId DriverId, int? Page = 1, int? PerPage = Constants.DEFAULT_PAGE_SIZE)
  : IQuery<Result<PagedResult<GrantDto>>>;

public class ListGrantsByDriverHandler(IListGrantsByDriverQueryService query)
  : IQueryHandler<ListGrantsByDriverQuery, Result<PagedResult<GrantDto>>>
{
  private readonly IListGrantsByDriverQueryService _query = query;

  public async ValueTask<Result<PagedResult<GrantDto>>> Handle(ListGrantsByDriverQuery request,
                                                                 CancellationToken cancellationToken)
  {
    var result = await _query.ListAsync(request.DriverId, request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE);

    return Result.Success(result);
  }
}
