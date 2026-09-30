using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Features.Lots;
using Parkin.Api.Features.Lots.List;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListLotsQueryService(AppDbContext db) : IListLotsQueryService
{
  public async Task<PagedResult<LotResponse>> ListAsync(ListLotsQuery query, CancellationToken cancellationToken)
  {
    var lots = FilterByStatus(db.ParkingLots, query.Status);

    if (!string.IsNullOrWhiteSpace(query.Search))
    {
      var pattern = LikePattern.Containing(query.Search.Trim());
      lots = lots.Where(l => EF.Functions.ILike(l.Name, pattern) ||
        (l.Address != null && EF.Functions.ILike(l.Address, pattern)));
    }

    var rows = await lots
      .OrderBy(l => l.Name)
      .Skip((query.Page - 1) * query.PerPage)
      .Take(query.PerPage)
      .Select(l => new
      {
        l.Id,
        l.Name,
        l.Address,
        l.Timezone,
        l.AccessMode,
        l.FullBehavior,
        l.Status,
        Capacity = l.Spaces.Count(s => s.Status == SpaceStatus.Active && s.Type == SpaceType.General),
        l.Layout,
      })
      .AsNoTracking()
      .ToListAsync(cancellationToken);

    var items = rows
      .Select(row => new LotResponse(row.Id.Value, row.Name, row.Address, row.Timezone, row.AccessMode,
        row.FullBehavior, row.Status, row.Capacity, LotLayoutResponse.From(row.Layout)))
      .ToList();

    var totalCount = await lots.CountAsync(cancellationToken);
    var totalPages = (int)Math.Ceiling(totalCount / (double)query.PerPage);

    return new PagedResult<LotResponse>(items, query.Page, query.PerPage, totalCount, totalPages);
  }

  private static IQueryable<ParkingLot> FilterByStatus(IQueryable<ParkingLot> lots, LotStatusFilter? status) =>
    status switch
    {
      LotStatusFilter.All => lots,
      LotStatusFilter.Archived => lots.Where(l => l.Status == LotStatus.Archived),
      _ => lots.Where(l => l.Status == LotStatus.Active),
    };
}
