using Parkin.Api.Domain.Services;

namespace Parkin.Api.OccupancyFeatures.ListLotOccupancy;

public record ListLotOccupancyQuery : IQuery<Result<IReadOnlyList<LotOccupancyDto>>>;

public class ListLotOccupancyHandler(
  IActiveLotOccupancyQueryService query,
  IOccupancyCalculator occupancyCalculator)
  : IQueryHandler<ListLotOccupancyQuery, Result<IReadOnlyList<LotOccupancyDto>>>
{
  public async ValueTask<Result<IReadOnlyList<LotOccupancyDto>>> Handle(
    ListLotOccupancyQuery request, CancellationToken cancellationToken)
  {
    var inputs = await query.ListAsync(cancellationToken);
    var asOf = DateTimeOffset.UtcNow;

    var occupancies = inputs
      .Select(lot => ToDto(lot, asOf))
      .ToList();

    return Result.Success<IReadOnlyList<LotOccupancyDto>>(occupancies);
  }

  private LotOccupancyDto ToDto(LotOccupancyInputs lot, DateTimeOffset asOf)
  {
    var occupancy = occupancyCalculator.Calculate(
      OccupancyContext.Create(lot.GeneralCapacity, lot.ActiveGeneralSessions, lot.ActiveReservedSessions));

    return new LotOccupancyDto(
      lot.LotId,
      occupancy.GeneralCapacity,
      occupancy.GeneralUsed,
      occupancy.GeneralFree,
      occupancy.IsGeneralPoolFull,
      occupancy.IsOverCapacity,
      lot.ReservedSpaceCount,
      occupancy.ReservedCount,
      asOf,
      lot.LotName);
  }
}
