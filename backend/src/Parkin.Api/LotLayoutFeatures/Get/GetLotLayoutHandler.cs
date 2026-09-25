using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.LotLayoutFeatures.Get;

public record GetLotLayoutQuery(ParkingLotId LotId) : IQuery<Result<LotLayoutViewDto>>;

public class GetLotLayoutHandler(ILotLayoutQueryService queryService)
  : IQueryHandler<GetLotLayoutQuery, Result<LotLayoutViewDto>>
{
  public async ValueTask<Result<LotLayoutViewDto>> Handle(GetLotLayoutQuery request, CancellationToken cancellationToken)
  {
    var view = await queryService.GetAsync(request.LotId, cancellationToken);

    return view is null ? Result.NotFound() : view;
  }
}
