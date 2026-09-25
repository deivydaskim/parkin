namespace Parkin.Api.OccupancyFeatures.ListLotOccupancy;

public interface IActiveLotOccupancyQueryService
{
  Task<IReadOnlyList<LotOccupancyInputs>> ListAsync(CancellationToken cancellationToken);
}
