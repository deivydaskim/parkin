using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.AccessEvents.List;

public record ListAccessEventsQuery(ParkingLotId LotId, int Page, int PerPage)
  : IQuery<Result<PagedResult<AccessEventListItemResponse>>>;

public class ListAccessEventsHandler(
  IReadRepository<ParkingLot> lotRepository,
  IListAccessEventsQueryService query)
  : IQueryHandler<ListAccessEventsQuery, Result<PagedResult<AccessEventListItemResponse>>>
{
  public async ValueTask<Result<PagedResult<AccessEventListItemResponse>>> Handle(
    ListAccessEventsQuery request, CancellationToken cancellationToken)
  {
    var lot = await lotRepository.GetByIdAsync(request.LotId, cancellationToken);
    if (lot is null) return Result.NotFound();

    return await query.ListByLotAsync(request.LotId, request.Page, request.PerPage, cancellationToken);
  }
}
