using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.LotLayouts.Get;

public interface ILotLayoutQueryService
{
  Task<LotLayoutViewResponse?> GetAsync(ParkingLotId lotId, CancellationToken cancellationToken);
}
