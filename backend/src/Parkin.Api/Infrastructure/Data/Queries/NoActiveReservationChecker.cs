using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.SpaceFeatures;

namespace Parkin.Api.Infrastructure.Data.Queries;

public class NoActiveReservationChecker : IActiveReservationChecker
{
  // TODO(T4): replace with a real check against the Reservation aggregate once it exists.
  public Task<bool> HasActiveReservationAsync(ParkingSpaceId spaceId, CancellationToken cancellationToken)
    => Task.FromResult(false);
}
