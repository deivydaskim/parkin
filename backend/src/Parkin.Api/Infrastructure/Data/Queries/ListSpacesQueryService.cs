using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.SpaceFeatures;
using Parkin.Api.SpaceFeatures.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListSpacesQueryService(AppDbContext db) : IListSpacesQueryService
{
  private readonly AppDbContext _db = db;

  public async Task<PagedResult<SpaceDto>> ListAsync(Guid lotId, int page, int perPage, SpaceStatusFilter? status,
    SpaceType? type = null, string? search = null)
  {
    var query = _db.ParkingSpaces.Where(s => s.LotId == ParkingLotId.From(lotId));

    query = status switch
    {
      SpaceStatusFilter.All => query,
      SpaceStatusFilter.Inactive => query.Where(s => s.Status == SpaceStatus.Inactive),
      _ => query.Where(s => s.Status == SpaceStatus.Active),
    };

    if (type.HasValue)
      query = query.Where(s => s.Type == type.Value);

    if (!string.IsNullOrWhiteSpace(search))
    {
      var pattern = LikePattern.Containing(search.Trim());
      query = query.Where(s => EF.Functions.ILike(s.Label, pattern));
    }

    var items = await query
      .OrderBy(s => s.Label)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(s => new
      {
        s.Id,
        s.LotId,
        s.Label,
        s.Type,
        s.Status,
        s.Zone,
        s.Placement,
        Holder = _db.Reservations
          .Where(r => r.SpaceId == s.Id && r.Status == ReservationStatus.Active)
          .Join(_db.Drivers, r => r.DriverId, d => d.Id, (r, d) => new { d.Id, d.Name })
          .FirstOrDefault(),
      })
      .AsNoTracking()
      .ToListAsync();

    var dtos = items
      .Select(row => new SpaceDto(row.Id, row.LotId, row.Label, row.Type, row.Status, row.Zone, row.Placement,
        row.Holder?.Id, row.Holder?.Name))
      .ToList();

    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<SpaceDto>(dtos, page, perPage, totalCount, totalPages);
  }
}
