using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.GrantFeatures;
using Parkin.Api.GrantFeatures.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListGrantsByDriverQueryService(AppDbContext db) : IListGrantsByDriverQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<GrantDto>> ListAsync(DriverId driverId, int page, int perPage)
  {
    var query = _db.AccessGrants.Where(g => g.DriverId == driverId);

    var items = await query
      .OrderByDescending(g => g.CreatedAt)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(g => new GrantDto(g.Id, g.DriverId, g.ParkingLotId, g.ValidFrom, g.ValidTo, g.Status))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
    var result = new PagedResult<GrantDto>(items, page, perPage, totalCount, totalPages);

    return result;
  }
}
