using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SpaceFeatures.List;

public record ListSpacesQuery(Guid LotId,
  int? Page = 1,
  int? PerPage = Constants.DEFAULT_PAGE_SIZE,
  SpaceStatusFilter? Status = null,
  SpaceType? Type = null,
  string? Search = null)
  : IQuery<Result<PagedResult<SpaceDto>>>;

public class ListSpacesHandler(IListSpacesQueryService query) : IQueryHandler<ListSpacesQuery, Result<PagedResult<SpaceDto>>>
{
  private readonly IListSpacesQueryService _query = query;

  public async ValueTask<Result<PagedResult<SpaceDto>>> Handle(ListSpacesQuery request,
                                                                 CancellationToken cancellationToken)
  {
    var result = await _query.ListAsync(request.LotId, request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE, request.Status,
      request.Type, request.Search);

    return Result.Success(result);
  }
}
