using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Grants.List;

public interface IListGrantsByDriverQueryService
{
  Task<PagedResult<GrantDto>> ListAsync(DriverId driverId, int page, int perPage);
}
