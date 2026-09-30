using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Features.Sessions.ListActiveByLot;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListActiveSessionsByLotQueryService(AppDbContext db) : IListActiveSessionsByLotQueryService
{
  public async Task<PagedResult<ActiveSessionResponse>> ListAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken)
  {
    var query = db.ParkingSessions
      .AsNoTracking()
      .Where(s => s.LotId == lotId && s.Status == SessionStatus.Active);

    var sessions = await query
      .OrderByDescending(s => s.EntryTime)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(s => new { s.Id, s.Plate, s.DriverId, s.Pool, s.SpaceId, s.EntryTime })
      .ToListAsync(cancellationToken);

    var driverIds = sessions
      .Where(s => s.DriverId.HasValue)
      .Select(s => s.DriverId!.Value)
      .Distinct()
      .ToList();
    var driverNameById = await db.Drivers
      .AsNoTracking()
      .Where(d => driverIds.Contains(d.Id))
      .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

    var spaceIds = sessions
      .Where(s => s.SpaceId.HasValue)
      .Select(s => s.SpaceId!.Value)
      .Distinct()
      .ToList();
    var spaceLabelById = await db.ParkingSpaces
      .AsNoTracking()
      .Where(s => spaceIds.Contains(s.Id))
      .ToDictionaryAsync(s => s.Id, s => s.Label, cancellationToken);

    var items = sessions
      .Select(s => new ActiveSessionResponse(
        s.Id.Value,
        s.Plate,
        s.DriverId?.Value,
        s.DriverId is { } driverId ? driverNameById.GetValueOrDefault(driverId) : null,
        s.Pool,
        s.SpaceId?.Value,
        s.SpaceId is { } spaceId ? spaceLabelById.GetValueOrDefault(spaceId) : null,
        s.EntryTime))
      .ToList();

    int totalCount = await query.CountAsync(cancellationToken);
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<ActiveSessionResponse>(items, page, perPage, totalCount, totalPages);
  }
}
