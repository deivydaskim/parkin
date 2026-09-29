using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Spaces.List;

public interface IListSpacesQueryService
{
  Task<PagedResult<SpaceDto>> ListAsync(Guid lotId, int page, int perPage, SpaceStatusFilter? status,
    SpaceType? type = null, string? search = null);
}
