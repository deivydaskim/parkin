using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.LotFeatures.Restore;

public record RestoreLotCommand(ParkingLotId LotId, Guid? ActorId) : ICommand<Result<LotDto>>;

public class RestoreLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<RestoreLotCommand, Result<LotDto>>
{
  public async ValueTask<Result<LotDto>> Handle(RestoreLotCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot == null) return Result.NotFound();

    var duplicate = await repository.FirstOrDefaultAsync(new ParkingLotByNameSpec(lot.Name), cancellationToken);
    if (duplicate != null && duplicate.Id != lot.Id)
    {
      return Result.Invalid(new ValidationError("Name", "A lot with this name already exists"));
    }

    lot.Restore(request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    return new LotDto(lot.Id, lot.Name, lot.Address, lot.Timezone, lot.AccessMode, lot.FullBehavior, lot.Status, lot.Capacity);
  }
}
