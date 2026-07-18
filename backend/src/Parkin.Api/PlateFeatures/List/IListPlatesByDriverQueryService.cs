using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.PlateFeatures.List;

public interface IListPlatesByDriverQueryService
{
  Task<PagedResult<PlateDto>> ListAsync(DriverId driverId, int page, int perPage);
}
