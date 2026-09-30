using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.LotLayouts.Get;

public record GetLotLayoutQuery(ParkingLotId LotId) : IQuery<Result<LotLayoutViewResponse>>;

public class GetLotLayoutHandler(ILotLayoutQueryService queryService)
  : IQueryHandler<GetLotLayoutQuery, Result<LotLayoutViewResponse>>
{
  public async ValueTask<Result<LotLayoutViewResponse>> Handle(GetLotLayoutQuery request,
    CancellationToken cancellationToken)
  {
    var view = await queryService.GetAsync(request.LotId, cancellationToken);

    return view is null ? Result.NotFound() : view;
  }
}
