using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Lots.Update;

public record UpdateLotCommand(
  ParkingLotId LotId,
  string? Name,
  string? Address,
  string? Timezone,
  AccessMode? AccessMode,
  FullBehavior? FullBehavior,
  Guid? ActorId,
  LotLayout? Layout = null,
  bool ClearLayout = false) : ICommand<Result<LotResponse>>;

public class UpdateLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<UpdateLotCommand, Result<LotResponse>>
{
  public async ValueTask<Result<LotResponse>> Handle(UpdateLotCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null) return Result.NotFound();

    var name = request.Name ?? lot.Name;
    if (name != lot.Name
      && await repository.AnyAsync(new ParkingLotByNameSpec(name, excludingLotId: lot.Id), cancellationToken))
    {
      return Result.Invalid(LotErrors.DuplicateName);
    }

    var layout = request.ClearLayout ? null : request.Layout ?? lot.Layout;
    var detailsResult = lot.UpdateDetails(name, request.Address ?? lot.Address, request.Timezone ?? lot.Timezone,
      layout, request.ActorId);
    if (!detailsResult.IsSuccess) return detailsResult;

    if (request.AccessMode is { } accessMode) lot.SetAccessMode(accessMode, request.ActorId);
    if (request.FullBehavior is { } fullBehavior) lot.SetFullBehavior(fullBehavior, request.ActorId);

    await repository.UpdateAsync(lot, cancellationToken);

    return LotResponse.From(lot);
  }
}
