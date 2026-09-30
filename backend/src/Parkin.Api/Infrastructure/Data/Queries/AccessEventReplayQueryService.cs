using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.AccessEventAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.Features.AccessEvents;
using Parkin.Api.Features.AccessEvents.Ingest;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class AccessEventReplayQueryService(AppDbContext db) : IAccessEventReplayQueryService
{
  public async Task<RecordedAccessEvent?> FindAsync(Guid actorId, string idempotencyKey,
    CancellationToken cancellationToken)
  {
    var recorded = await db.AccessEvents
      .AsNoTracking()
      .Where(e => e.ActorId == actorId && e.IdempotencyKey == idempotencyKey)
      .Select(e => new
      {
        e.Id,
        e.LotId,
        e.NormalizedPlate,
        e.Direction,
        e.Decision,
        e.DenyReason,
        e.SessionId,
        e.OccurredAt
      })
      .FirstOrDefaultAsync(cancellationToken);
    if (recorded is null) return null;

    SessionPool? pool = null;
    string? reservedSpaceLabel = null;

    if (recorded.SessionId is { } sessionId)
    {
      var session = await db.ParkingSessions
        .AsNoTracking()
        .Where(s => s.Id == sessionId)
        .Select(s => new { s.Pool, s.SpaceId })
        .FirstOrDefaultAsync(cancellationToken);
      pool = session?.Pool;

      if (recorded.Direction == Direction.Enter && session?.SpaceId is { } spaceId)
      {
        reservedSpaceLabel = await db.ParkingSpaces
          .AsNoTracking()
          .Where(space => space.Id == spaceId)
          .Select(space => space.Label)
          .FirstOrDefaultAsync(cancellationToken);
      }
    }

    var decision = new AccessEventDecisionResponse(recorded.Id.Value, recorded.Decision, recorded.DenyReason, pool,
      reservedSpaceLabel, recorded.SessionId?.Value, recorded.OccurredAt);

    return new RecordedAccessEvent(recorded.LotId, recorded.NormalizedPlate, recorded.Direction, decision);
  }
}
