using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.LotFeatures;
using Parkin.Api.LotFeatures.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListLotsQueryService(AppDbContext db) : IListLotsQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<LotDto>> ListAsync(int page, int perPage, string? status)
  {
    var query = _db.ParkingLots.AsQueryable();

    query = status?.ToUpperInvariant() switch
    {
      "ALL" => query,
      "ARCHIVED" => query.Where(l => l.Status == LotStatus.Archived),
      _ => query.Where(l => l.Status == LotStatus.Active), // default: active-only
    };

    var items = await query
      .OrderBy(l => l.Name)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(l => new LotDto(l.Id, l.Name, l.Address, l.Timezone, l.AccessMode, l.FullBehavior, l.Status))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
    var result = new PagedResult<LotDto>(items, page, perPage, totalCount, totalPages);

    return result;
  }
}
