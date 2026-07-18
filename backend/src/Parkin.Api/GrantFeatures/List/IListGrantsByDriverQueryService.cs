using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.GrantFeatures.List;

public interface IListGrantsByDriverQueryService
{
  Task<PagedResult<GrantDto>> ListAsync(DriverId driverId, int page, int perPage);
}
