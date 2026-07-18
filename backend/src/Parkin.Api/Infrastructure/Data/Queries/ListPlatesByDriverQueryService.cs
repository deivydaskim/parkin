using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.PlateFeatures;
using Parkin.Api.PlateFeatures.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListPlatesByDriverQueryService(AppDbContext db) : IListPlatesByDriverQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<PlateDto>> ListAsync(DriverId driverId, int page, int perPage)
  {
    var query = _db.Plates.Where(p => p.DriverId == driverId);

    var items = await query
      .OrderBy(p => p.NormalizedPlateNumber)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(p => new PlateDto(p.Id, p.DriverId, p.NormalizedPlateNumber, p.Status))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
    var result = new PagedResult<PlateDto>(items, page, perPage, totalCount, totalPages);

    return result;
  }
}
