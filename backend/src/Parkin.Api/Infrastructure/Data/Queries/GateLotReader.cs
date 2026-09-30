using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.Services;
using Parkin.Api.Features.AccessEvents.Ingest;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class GateLotReader(AppDbContext db, ILotRowLocker lotRowLocker) : IGateLotReader
{
  public async Task<GateLot?> LockAndLoadAsync(ParkingLotId lotId, CancellationToken cancellationToken)
  {
    await lotRowLocker.LockAsync(lotId, cancellationToken);

    return await db.ParkingLots
      .AsNoTracking()
      .Where(lot => lot.Id == lotId)
      .Select(lot => new GateLot(
        lot.Id,
        lot.Status,
        lot.AccessMode,
        lot.FullBehavior,
        lot.Spaces.Count(space => space.Status == SpaceStatus.Active && space.Type == SpaceType.General),
        db.ParkingSessions.Count(session => session.LotId == lotId &&
          session.Status == SessionStatus.Active && session.Pool == SessionPool.General),
        db.ParkingSessions.Count(session => session.LotId == lotId &&
          session.Status == SessionStatus.Active && session.Pool == SessionPool.Reserved)))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public Task<string?> FindActiveSpaceLabelAsync(ParkingLotId lotId, ParkingSpaceId spaceId,
    CancellationToken cancellationToken)
    => db.ParkingSpaces
      .AsNoTracking()
      .Where(space => space.Id == spaceId && space.LotId == lotId && space.Status == SpaceStatus.Active)
      .Select(space => (string?)space.Label)
      .FirstOrDefaultAsync(cancellationToken);
}
