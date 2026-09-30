using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Occupancy;

public interface ILotOccupancyQueryService
{
  Task<LotOccupancyInputs?> FindAsync(ParkingLotId lotId, CancellationToken cancellationToken);

  Task<IReadOnlyList<LotOccupancyInputs>> ListActiveAsync(CancellationToken cancellationToken);
}
