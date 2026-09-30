using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Lots.GetById;

public record GetLotQuery(ParkingLotId LotId) : IQuery<Result<LotResponse>>;

public class GetLotHandler(IReadRepository<ParkingLot> repository)
  : IQueryHandler<GetLotQuery, Result<LotResponse>>
{
  public async ValueTask<Result<LotResponse>> Handle(GetLotQuery request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null) return Result.NotFound();

    return LotResponse.From(lot);
  }
}
