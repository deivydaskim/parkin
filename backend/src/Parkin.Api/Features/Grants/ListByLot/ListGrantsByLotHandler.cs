using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Grants.ListByLot;

public record ListGrantsByLotQuery(ParkingLotId LotId, int? Page = 1, int? PerPage = Constants.DEFAULT_PAGE_SIZE)
  : IQuery<Result<PagedResult<GrantDto>>>;

public class ListGrantsByLotHandler(IListGrantsByLotQueryService query)
  : IQueryHandler<ListGrantsByLotQuery, Result<PagedResult<GrantDto>>>
{
  public async ValueTask<Result<PagedResult<GrantDto>>> Handle(ListGrantsByLotQuery request,
                                                                 CancellationToken cancellationToken)
  {
    var result = await query.ListAsync(request.LotId, request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE);

    return Result.Success(result);
  }
}
