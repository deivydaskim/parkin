using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.Features.Occupancy;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class LotOccupancyQueryService(AppDbContext db) : ILotOccupancyQueryService
{
  public async Task<LotOccupancyInputs?> FindAsync(ParkingLotId lotId, CancellationToken cancellationToken)
  {
    var inputs = await ListAsync(db.ParkingLots.Where(lot => lot.Id == lotId), cancellationToken);
    return inputs.SingleOrDefault();
  }

  public Task<IReadOnlyList<LotOccupancyInputs>> ListActiveAsync(CancellationToken cancellationToken)
    => ListAsync(db.ParkingLots.Where(lot => lot.Status == LotStatus.Active), cancellationToken);

  private async Task<IReadOnlyList<LotOccupancyInputs>> ListAsync(IQueryable<ParkingLot> lotsQuery,
    CancellationToken cancellationToken)
  {
    var lots = await lotsQuery
      .AsNoTracking()
      .OrderBy(lot => lot.Name)
      .Select(lot => new
      {
        lot.Id,
        lot.Name,
        GeneralCapacity = lot.Spaces.Count(s => s.Status == SpaceStatus.Active && s.Type == SpaceType.General),
        ReservedSpaceCount = lot.Spaces.Count(s => s.Status == SpaceStatus.Active && s.Type == SpaceType.Reserved),
      })
      .ToListAsync(cancellationToken);

    var lotIds = lots.Select(lot => lot.Id).ToList();

    var sessionCounts = await db.ParkingSessions
      .AsNoTracking()
      .Where(session => session.Status == SessionStatus.Active && lotIds.Contains(session.LotId))
      .GroupBy(session => new { session.LotId, session.Pool })
      .Select(group => new { group.Key.LotId, group.Key.Pool, Count = group.Count() })
      .ToListAsync(cancellationToken);

    int CountFor(ParkingLotId lotId, SessionPool pool) =>
      sessionCounts.FirstOrDefault(c => c.LotId == lotId && c.Pool == pool)?.Count ?? 0;

    return lots
      .Select(lot => new LotOccupancyInputs(
        lot.Id,
        lot.Name,
        lot.GeneralCapacity,
        lot.ReservedSpaceCount,
        CountFor(lot.Id, SessionPool.General),
        CountFor(lot.Id, SessionPool.Reserved)))
      .ToList();
  }
}
