using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.GrantFeatures.ListByLot;

public interface IListGrantsByLotQueryService
{
  Task<PagedResult<GrantDto>> ListAsync(ParkingLotId lotId, int page, int perPage);
}
