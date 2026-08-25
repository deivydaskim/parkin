using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.Interfaces;

// Row-level write lock on one lot for the rest of the ambient transaction, serializing
// "read occupancy -> decide -> open session" so a BLOCK lot cannot be over-filled.
public interface ILotRowLocker
{
  Task LockAsync(ParkingLotId lotId, CancellationToken cancellationToken);
}
