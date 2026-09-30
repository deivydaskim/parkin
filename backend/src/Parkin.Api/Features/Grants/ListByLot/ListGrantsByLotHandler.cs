using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Grants.ListByLot;

public record ListGrantsByLotQuery(ParkingLotId LotId, int Page, int PerPage)
  : IQuery<Result<PagedResult<GrantResponse>>>;

public class ListGrantsByLotHandler(IListGrantsByLotQueryService query)
  : IQueryHandler<ListGrantsByLotQuery, Result<PagedResult<GrantResponse>>>
{
  public async ValueTask<Result<PagedResult<GrantResponse>>> Handle(ListGrantsByLotQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request.LotId, request.Page, request.PerPage, cancellationToken);
}
