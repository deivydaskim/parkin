using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.GrantFeatures;
using Parkin.Api.GrantFeatures.ListByLot;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListGrantsByLotQueryService(AppDbContext db) : IListGrantsByLotQueryService
{
  public async Task<PagedResult<GrantDto>> ListAsync(ParkingLotId lotId, int page, int perPage)
  {
    var query = db.AccessGrants.Where(g => g.ParkingLotId == lotId);

    var items = await query
      .OrderByDescending(g => g.CreatedAt)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(g => new GrantDto(g.Id, g.DriverId, g.ParkingLotId, g.ValidFrom, g.ValidTo, g.Status,
        null,
        db.Drivers.Where(d => d.Id == g.DriverId).Select(d => d.Name).FirstOrDefault()))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<GrantDto>(items, page, perPage, totalCount, totalPages);
  }
}
