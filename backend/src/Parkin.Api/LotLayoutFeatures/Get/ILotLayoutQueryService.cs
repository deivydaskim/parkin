using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.LotLayoutFeatures.Get;

public interface ILotLayoutQueryService
{
  Task<LotLayoutViewDto?> GetAsync(ParkingLotId lotId, CancellationToken cancellationToken);
}
