using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.PlateFeatures.List;

public record ListPlatesByDriverQuery(DriverId DriverId, int? Page = 1, int? PerPage = Constants.DEFAULT_PAGE_SIZE)
  : IQuery<Result<PagedResult<PlateDto>>>;

public class ListPlatesByDriverHandler(IListPlatesByDriverQueryService query)
  : IQueryHandler<ListPlatesByDriverQuery, Result<PagedResult<PlateDto>>>
{
  private readonly IListPlatesByDriverQueryService _query = query;

  public async ValueTask<Result<PagedResult<PlateDto>>> Handle(ListPlatesByDriverQuery request,
                                                                 CancellationToken cancellationToken)
  {
    var result = await _query.ListAsync(request.DriverId, request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE);

    return Result.Success(result);
  }
}
