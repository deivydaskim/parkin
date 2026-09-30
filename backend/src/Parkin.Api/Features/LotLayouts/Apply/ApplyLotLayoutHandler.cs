using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Features.LotLayouts.Get;

namespace Parkin.Api.Features.LotLayouts.Apply;

public record ApplyLotLayoutCommand(
  ParkingLotId LotId,
  LotLayout? Layout,
  bool ClearLayout,
  IReadOnlyList<SpaceLayoutChange> Spaces,
  Guid? ActorId) : ICommand<Result<LotLayoutViewResponse>>;

public class ApplyLotLayoutHandler(IRepository<ParkingLot> repository, ILotLayoutQueryService layoutQuery)
  : ICommandHandler<ApplyLotLayoutCommand, Result<LotLayoutViewResponse>>
{
  public async ValueTask<Result<LotLayoutViewResponse>> Handle(ApplyLotLayoutCommand request,
    CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null) return Result.NotFound();

    var layout = request.ClearLayout ? null : request.Layout ?? lot.Layout;
    var result = lot.ApplyLayout(layout, request.Spaces, request.ActorId);
    if (!result.IsSuccess) return result;

    await repository.UpdateAsync(lot, cancellationToken);

    var view = await layoutQuery.GetAsync(lot.Id, cancellationToken);
    return view is null ? Result.NotFound() : view;
  }
}
