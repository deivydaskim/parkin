using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Grants.ListByLot;

public interface IListGrantsByLotQueryService
{
  Task<PagedResult<GrantDto>> ListAsync(ParkingLotId lotId, int page, int perPage);
}
