using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.LotLayoutFeatures.Apply;

public record ApplyLotLayoutCommand(
  ParkingLotId LotId,
  LotLayout? Layout,
  bool ClearLayout,
  IReadOnlyList<SpaceLayoutChange> Spaces,
  Guid? ActorId) : ICommand<Result>;

public class ApplyLotLayoutHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<ApplyLotLayoutCommand, Result>
{
  public async ValueTask<Result> Handle(ApplyLotLayoutCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot == null) return Result.NotFound();

    var layout = request.ClearLayout ? null : request.Layout ?? lot.Layout;
    var result = lot.ApplyLayout(layout, request.Spaces, request.ActorId);
    if (!result.IsSuccess) return result;

    await repository.UpdateAsync(lot, cancellationToken);
    return Result.Success();
  }
}
