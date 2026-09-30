using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Features.Plates;
using Parkin.Api.Features.Plates.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListPlatesByDriverQueryService(AppDbContext db) : IListPlatesByDriverQueryService
{
  public Task<PagedResult<PlateResponse>> ListAsync(DriverId driverId, int page, int perPage,
    CancellationToken cancellationToken)
    => db.Plates.AsNoTracking()
      .Where(plate => plate.DriverId == driverId)
      .OrderBy(plate => plate.NormalizedPlateNumber)
      .Select(plate => new PlateResponse(
        plate.Id.Value, plate.DriverId.Value, plate.NormalizedPlateNumber, plate.Status))
      .ToPagedResultAsync(page, perPage, cancellationToken);
}
