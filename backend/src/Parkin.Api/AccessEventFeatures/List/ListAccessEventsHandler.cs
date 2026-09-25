using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.AccessEventFeatures.List;

public record ListAccessEventsQuery(ParkingLotId LotId, int Page, int PerPage)
  : IQuery<Result<PagedResult<AccessEventListItemDto>>>;

public class ListAccessEventsHandler(
  IReadRepository<ParkingLot> lotRepository,
  IListAccessEventsQueryService query)
  : IQueryHandler<ListAccessEventsQuery, Result<PagedResult<AccessEventListItemDto>>>
{
  public async ValueTask<Result<PagedResult<AccessEventListItemDto>>> Handle(
    ListAccessEventsQuery request, CancellationToken cancellationToken)
  {
    var lotExists = await lotRepository.AnyAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (!lotExists) return Result.NotFound();

    return await query.ListByLotAsync(request.LotId, request.Page, request.PerPage, cancellationToken);
  }
}
