using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class LotRowLocker(AppDbContext dbContext) : ILotRowLocker
{
  // Only meaningful inside a transaction - the lock is held until commit/rollback.
  public Task LockAsync(ParkingLotId lotId, CancellationToken cancellationToken)
    => dbContext.Database.ExecuteSqlAsync(
      $"SELECT 1 FROM \"ParkingLots\" WHERE \"Id\" = {lotId.Value} FOR UPDATE", cancellationToken);
}
