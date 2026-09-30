using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Plates.List;

public interface IListPlatesByDriverQueryService
{
  Task<PagedResult<PlateResponse>> ListAsync(DriverId driverId, int page, int perPage,
    CancellationToken cancellationToken);
}
