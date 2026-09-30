using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Sessions.ListActiveByLot;

public record ListActiveSessionsByLotQuery(ParkingLotId LotId, int Page, int PerPage)
  : IQuery<Result<PagedResult<ActiveSessionResponse>>>;

public class ListActiveSessionsByLotHandler(IListActiveSessionsByLotQueryService query)
  : IQueryHandler<ListActiveSessionsByLotQuery, Result<PagedResult<ActiveSessionResponse>>>
{
  public async ValueTask<Result<PagedResult<ActiveSessionResponse>>> Handle(ListActiveSessionsByLotQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request.LotId, request.Page, request.PerPage, cancellationToken);
}
