using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Domain.Interfaces;

public interface ILotRowLocker
{
  Task LockAsync(ParkingLotId lotId, CancellationToken cancellationToken);
}
