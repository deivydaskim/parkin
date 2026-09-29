namespace Parkin.Api.Features.Occupancy.ListLotOccupancy;

public interface IActiveLotOccupancyQueryService
{
  Task<IReadOnlyList<LotOccupancyInputs>> ListAsync(CancellationToken cancellationToken);
}
