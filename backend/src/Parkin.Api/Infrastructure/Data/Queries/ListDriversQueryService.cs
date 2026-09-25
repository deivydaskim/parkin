using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.DriverFeatures;
using Parkin.Api.DriverFeatures.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListDriversQueryService(AppDbContext db) : IListDriversQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<DriverDto>> ListAsync(int page, int perPage, DriverStatusFilter? status, string? search = null)
  {
    var query = _db.Drivers.AsQueryable();

    query = status switch
    {
      DriverStatusFilter.All => query,
      DriverStatusFilter.Archived => query.Where(d => d.Status == DriverStatus.Archived),
      _ => query.Where(d => d.Status == DriverStatus.Active), // default: active-only
    };

    if (!string.IsNullOrWhiteSpace(search))
    {
      var pattern = LikePattern.Containing(search.Trim());
      var platePattern = LikePattern.Containing(PlateNormalizer.Normalize(search));
      query = query.Where(d => EF.Functions.ILike(d.Name, pattern) ||
        (d.Contact != null && EF.Functions.ILike(d.Contact, pattern)) ||
        d.Plates.Any(p => EF.Functions.ILike(p.NormalizedPlateNumber, platePattern)));
    }

    var items = await query
      .OrderBy(d => d.Name)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(d => new DriverDto(d.Id, d.Name, d.Contact, d.Status, d.Plates.Count))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
    var result = new PagedResult<DriverDto>(items, page, perPage, totalCount, totalPages);

    return result;
  }
}
