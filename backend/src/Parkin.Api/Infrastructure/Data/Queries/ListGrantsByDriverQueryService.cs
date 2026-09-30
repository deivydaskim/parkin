using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Features.Grants;
using Parkin.Api.Features.Grants.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListGrantsByDriverQueryService(AppDbContext db) : IListGrantsByDriverQueryService
{
  public Task<PagedResult<GrantResponse>> ListAsync(DriverId driverId, int page, int perPage,
    CancellationToken cancellationToken)
    => db.AccessGrants.AsNoTracking()
      .Where(grant => grant.DriverId == driverId)
      .OrderByDescending(grant => grant.CreatedAt)
      .Select(grant => new GrantResponse(
        grant.Id.Value, grant.DriverId.Value, grant.ParkingLotId.Value, grant.ValidFrom, grant.ValidTo, grant.Status,
        db.ParkingLots.Where(lot => lot.Id == grant.ParkingLotId).Select(lot => lot.Name).FirstOrDefault(),
        db.Drivers.Where(driver => driver.Id == grant.DriverId).Select(driver => driver.Name).FirstOrDefault()))
      .ToPagedResultAsync(page, perPage, cancellationToken);
}
