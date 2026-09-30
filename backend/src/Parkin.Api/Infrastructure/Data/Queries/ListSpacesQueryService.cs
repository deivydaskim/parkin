using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Features.Spaces;
using Parkin.Api.Features.Spaces.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListSpacesQueryService(AppDbContext db) : IListSpacesQueryService
{
  public async Task<PagedResult<SpaceResponse>> ListAsync(ListSpacesQuery query, CancellationToken cancellationToken)
  {
    var spaces = FilterByStatus(db.ParkingSpaces.Where(s => s.LotId == query.LotId), query.Status);

    if (query.Type.HasValue)
    {
      spaces = spaces.Where(s => s.Type == query.Type.Value);
    }

    if (!string.IsNullOrWhiteSpace(query.Search))
    {
      var pattern = LikePattern.Containing(query.Search.Trim());
      spaces = spaces.Where(s => EF.Functions.ILike(s.Label, pattern));
    }

    var rows = await spaces
      .OrderBy(s => s.Label)
      .Skip((query.Page - 1) * query.PerPage)
      .Take(query.PerPage)
      .Select(s => new
      {
        s.Id,
        s.LotId,
        s.Label,
        s.Type,
        s.Status,
        s.Zone,
        s.Placement,
        Holder = db.Reservations
          .Where(r => r.SpaceId == s.Id && r.Status == ReservationStatus.Active)
          .Join(db.Drivers, r => r.DriverId, d => d.Id, (r, d) => new { d.Id, d.Name })
          .FirstOrDefault(),
      })
      .AsNoTracking()
      .ToListAsync(cancellationToken);

    var items = rows
      .Select(row => new SpaceResponse(row.Id.Value, row.LotId.Value, row.Label, row.Type, row.Status, row.Zone,
        SpacePlacementResponse.From(row.Placement), row.Holder?.Id.Value, row.Holder?.Name))
      .ToList();

    var totalCount = await spaces.CountAsync(cancellationToken);
    var totalPages = (int)Math.Ceiling(totalCount / (double)query.PerPage);

    return new PagedResult<SpaceResponse>(items, query.Page, query.PerPage, totalCount, totalPages);
  }

  private static IQueryable<ParkingSpace> FilterByStatus(IQueryable<ParkingSpace> spaces, SpaceStatusFilter? status) =>
    status switch
    {
      SpaceStatusFilter.All => spaces,
      SpaceStatusFilter.Inactive => spaces.Where(s => s.Status == SpaceStatus.Inactive),
      _ => spaces.Where(s => s.Status == SpaceStatus.Active),
    };
}
