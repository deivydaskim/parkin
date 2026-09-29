using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Sessions.ListActiveByLot;

public interface IListActiveSessionsByLotQueryService
{
  Task<PagedResult<ActiveSessionDto>> ListAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken);
}
