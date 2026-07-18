using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.SpaceFeatures;
using Parkin.Api.SpaceFeatures.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListSpacesQueryService(AppDbContext db) : IListSpacesQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<SpaceDto>> ListAsync(Guid lotId, int page, int perPage, SpaceStatusFilter? status)
  {
    var query = _db.ParkingSpaces.Where(s => s.LotId == ParkingLotId.From(lotId));

    query = status switch
    {
      SpaceStatusFilter.All => query,
      SpaceStatusFilter.Inactive => query.Where(s => s.Status == SpaceStatus.Inactive),
      _ => query.Where(s => s.Status == SpaceStatus.Active), // default: active-only
    };

    var items = await query
      .OrderBy(s => s.Label)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(s => new SpaceDto(s.Id, s.LotId, s.Label, s.Type, s.Status))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
    var result = new PagedResult<SpaceDto>(items, page, perPage, totalCount, totalPages);

    return result;
  }
}
