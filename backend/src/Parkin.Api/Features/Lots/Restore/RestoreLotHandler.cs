using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Lots.Restore;

public record RestoreLotCommand(ParkingLotId LotId, Guid? ActorId) : ICommand<Result<LotResponse>>;

public class RestoreLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<RestoreLotCommand, Result<LotResponse>>
{
  public async ValueTask<Result<LotResponse>> Handle(RestoreLotCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null) return Result.NotFound();

    if (await repository.AnyAsync(new ParkingLotByNameSpec(lot.Name, excludingLotId: lot.Id), cancellationToken))
    {
      return Result.Invalid(LotErrors.DuplicateName);
    }

    lot.Restore(request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    return LotResponse.From(lot);
  }
}
