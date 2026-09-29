using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SessionFeatures.ListActiveByLot;

public interface IListActiveSessionsByLotQueryService
{
  Task<PagedResult<ActiveSessionDto>> ListAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken);
}
