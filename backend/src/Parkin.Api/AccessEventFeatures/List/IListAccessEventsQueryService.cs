using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.AccessEventFeatures.List;

public interface IListAccessEventsQueryService
{
  Task<PagedResult<AccessEventListItemDto>> ListByLotAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken);
}
