using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.SpaceFeatures;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class ActiveReservationChecker(AppDbContext dbContext) : IActiveReservationChecker
{
  public Task<bool> HasActiveReservationAsync(ParkingSpaceId spaceId, CancellationToken cancellationToken)
    => dbContext.Reservations.AnyAsync(
      r => r.SpaceId == spaceId && r.Status == ReservationStatus.Active, cancellationToken);
}
