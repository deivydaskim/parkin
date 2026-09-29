using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Sessions.ListActiveByLot;

public record ListActiveSessionsByLotQuery(ParkingLotId LotId, int? Page = 1, int? PerPage = Constants.DEFAULT_PAGE_SIZE)
  : IQuery<Result<PagedResult<ActiveSessionDto>>>;

public class ListActiveSessionsByLotHandler(IListActiveSessionsByLotQueryService query)
  : IQueryHandler<ListActiveSessionsByLotQuery, Result<PagedResult<ActiveSessionDto>>>
{
  public async ValueTask<Result<PagedResult<ActiveSessionDto>>> Handle(ListActiveSessionsByLotQuery request,
                                                                         CancellationToken cancellationToken)
  {
    var result = await query.ListAsync(request.LotId, request.Page ?? 1,
      request.PerPage ?? Constants.DEFAULT_PAGE_SIZE, cancellationToken);

    return Result.Success(result);
  }
}
