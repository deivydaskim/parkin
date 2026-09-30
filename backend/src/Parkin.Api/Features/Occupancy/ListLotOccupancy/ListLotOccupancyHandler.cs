namespace Parkin.Api.Features.Occupancy.ListLotOccupancy;

public record ListLotOccupancyQuery : IQuery<Result<IReadOnlyList<LotOccupancyResponse>>>;

public class ListLotOccupancyHandler(ILotOccupancyQueryService query, TimeProvider timeProvider)
  : IQueryHandler<ListLotOccupancyQuery, Result<IReadOnlyList<LotOccupancyResponse>>>
{
  public async ValueTask<Result<IReadOnlyList<LotOccupancyResponse>>> Handle(
    ListLotOccupancyQuery request, CancellationToken cancellationToken)
  {
    var lots = await query.ListActiveAsync(cancellationToken);
    var asOf = timeProvider.GetUtcNow();

    return Result.Success<IReadOnlyList<LotOccupancyResponse>>(
      lots.Select(lot => LotOccupancyResponse.From(lot, asOf)).ToList());
  }
}
