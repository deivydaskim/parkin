using Microsoft.EntityFrameworkCore;
using Parkin.Api.AccessEventFeatures.List;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ListAccessEventsQueryService(AppDbContext db) : IListAccessEventsQueryService
{
  public async Task<PagedResult<AccessEventListItemDto>> ListByLotAsync(ParkingLotId lotId, int page, int perPage,
    CancellationToken cancellationToken)
  {
    var query = db.AccessEvents.AsNoTracking().Where(e => e.LotId == lotId);

    var events = await query
      .OrderByDescending(e => e.OccurredAt)
      .ThenByDescending(e => e.ReceivedAt)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(e => new
      {
        e.Id,
        e.NormalizedPlate,
        e.Direction,
        e.Decision,
        e.DenyReason,
        e.Source,
        e.SessionId,
        e.MatchedDriverId,
        e.OccurredAt,
      })
      .ToListAsync(cancellationToken);

    var sessionIds = events
      .Where(e => e.SessionId.HasValue)
      .Select(e => e.SessionId!.Value)
      .Distinct()
      .ToList();
    var sessions = await db.ParkingSessions
      .AsNoTracking()
      .Where(s => sessionIds.Contains(s.Id))
      .Select(s => new { s.Id, s.Pool, s.SpaceId })
      .ToListAsync(cancellationToken);
    var sessionById = sessions.ToDictionary(s => s.Id);

    var spaceIds = sessions
      .Where(s => s.SpaceId.HasValue)
      .Select(s => s.SpaceId!.Value)
      .Distinct()
      .ToList();
    var spaceLabelById = await db.ParkingSpaces
      .AsNoTracking()
      .Where(s => spaceIds.Contains(s.Id))
      .ToDictionaryAsync(s => s.Id, s => s.Label, cancellationToken);

    var driverIds = events
      .Where(e => e.MatchedDriverId.HasValue)
      .Select(e => e.MatchedDriverId!.Value)
      .Distinct()
      .ToList();
    var driverNameById = await db.Drivers
      .AsNoTracking()
      .Where(d => driverIds.Contains(d.Id))
      .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

    var items = events
      .Select(e =>
      {
        var session = e.SessionId is { } sessionId ? sessionById.GetValueOrDefault(sessionId) : null;
        var spaceLabel = session?.SpaceId is { } spaceId ? spaceLabelById.GetValueOrDefault(spaceId) : null;
        var driverName = e.MatchedDriverId is { } driverId ? driverNameById.GetValueOrDefault(driverId) : null;

        return new AccessEventListItemDto(e.Id, e.NormalizedPlate, e.Direction, e.Decision, e.DenyReason,
          session?.Pool, spaceLabel, e.Source, e.MatchedDriverId, driverName, e.OccurredAt);
      })
      .ToList();

    int totalCount = await query.CountAsync(cancellationToken);
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<AccessEventListItemDto>(items, page, perPage, totalCount, totalPages);
  }
}
