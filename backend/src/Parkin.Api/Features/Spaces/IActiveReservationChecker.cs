using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Spaces;

public interface IActiveReservationChecker
{
  Task<bool> HasActiveReservationAsync(ParkingSpaceId spaceId, CancellationToken cancellationToken);
}
