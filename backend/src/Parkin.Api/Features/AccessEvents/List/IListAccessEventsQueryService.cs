using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.AccessEvents.List;

public interface IListAccessEventsQueryService
{
  Task<PagedResult<AccessEventListItemDto>> ListByLotAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken);
}
