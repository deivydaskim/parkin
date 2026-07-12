using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.LotFeatures.GetById;

public record GetLotQuery(ParkingLotId LotId) : IQuery<Result<LotDto>>;

public class GetLotHandler(IReadRepository<ParkingLot> repository)
  : IQueryHandler<GetLotQuery, Result<LotDto>>
{
  public async ValueTask<Result<LotDto>> Handle(GetLotQuery request, CancellationToken cancellationToken)
  {
    var spec = new ParkingLotByIdSpec(request.LotId);
    var entity = await repository.FirstOrDefaultAsync(spec, cancellationToken);
    if (entity == null) return Result.NotFound();

    return new LotDto(entity.Id, entity.Name, entity.Address, entity.Timezone, entity.AccessMode, entity.FullBehavior, entity.Status);
  }
}
