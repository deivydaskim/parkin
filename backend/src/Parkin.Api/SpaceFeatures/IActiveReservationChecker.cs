using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.SpaceFeatures;

public interface IActiveReservationChecker
{
  Task<bool> HasActiveReservationAsync(ParkingSpaceId spaceId, CancellationToken cancellationToken);
}
