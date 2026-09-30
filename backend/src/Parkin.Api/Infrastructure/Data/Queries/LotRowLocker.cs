using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class LotRowLocker(AppDbContext dbContext) : ILotRowLocker
{
  // The lock is held until the ambient transaction commits or rolls back; outside one it is released immediately.
  public Task LockAsync(ParkingLotId lotId, CancellationToken cancellationToken)
    => dbContext.Database.ExecuteSqlAsync(
      $"SELECT 1 FROM \"ParkingLots\" WHERE \"Id\" = {lotId.Value} FOR UPDATE", cancellationToken);
}
