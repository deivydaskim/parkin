using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Grants;
using Parkin.Api.Features.Grants.ListByLot;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListGrantsByLotQueryService(AppDbContext db) : IListGrantsByLotQueryService
{
  public Task<PagedResult<GrantResponse>> ListAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken)
    => db.AccessGrants.AsNoTracking()
      .Where(grant => grant.ParkingLotId == lotId)
      .OrderByDescending(grant => grant.CreatedAt)
      .Select(grant => new GrantResponse(
        grant.Id.Value, grant.DriverId.Value, grant.ParkingLotId.Value, grant.ValidFrom, grant.ValidTo, grant.Status,
        null,
        db.Drivers.Where(driver => driver.Id == grant.DriverId).Select(driver => driver.Name).FirstOrDefault()))
      .ToPagedResultAsync(page, perPage, cancellationToken);
}
