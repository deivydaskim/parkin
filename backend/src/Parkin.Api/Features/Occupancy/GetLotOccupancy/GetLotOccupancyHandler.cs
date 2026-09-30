using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Occupancy.GetLotOccupancy;

public record GetLotOccupancyQuery(ParkingLotId LotId) : IQuery<Result<LotOccupancyResponse>>;

public class GetLotOccupancyHandler(ILotOccupancyQueryService query, TimeProvider timeProvider)
  : IQueryHandler<GetLotOccupancyQuery, Result<LotOccupancyResponse>>
{
  public async ValueTask<Result<LotOccupancyResponse>> Handle(
    GetLotOccupancyQuery request, CancellationToken cancellationToken)
  {
    var lot = await query.FindAsync(request.LotId, cancellationToken);
    if (lot is null) return Result.NotFound();

    return LotOccupancyResponse.From(lot, timeProvider.GetUtcNow());
  }
}
