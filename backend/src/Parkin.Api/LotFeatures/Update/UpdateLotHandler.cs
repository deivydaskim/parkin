using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.LotFeatures.Update;

public record UpdateLotCommand(
  ParkingLotId LotId,
  string? Name,
  string? Address,
  string? Timezone,
  AccessMode? AccessMode,
  FullBehavior? FullBehavior) : ICommand<Result<LotDto>>;

public class UpdateLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<UpdateLotCommand, Result<LotDto>>
{
  public async ValueTask<Result<LotDto>> Handle(UpdateLotCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot == null) return Result.NotFound();

    if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != lot.Name)
    {
      var duplicate = await repository.FirstOrDefaultAsync(new ParkingLotByNameSpec(request.Name), cancellationToken);
      if (duplicate != null && duplicate.Id != lot.Id)
      {
        return Result.Invalid(new ValidationError("Name", "A lot with this name already exists"));
      }
    }

    var name = request.Name ?? lot.Name;
    var address = request.Address ?? lot.Address;
    var timezone = request.Timezone ?? lot.Timezone;
    lot.UpdateDetails(name, address, timezone);

    if (request.AccessMode.HasValue)
    {
      lot.SetAccessMode(request.AccessMode.Value);
    }

    if (request.FullBehavior.HasValue)
    {
      lot.SetFullBehavior(request.FullBehavior.Value);
    }

    await repository.UpdateAsync(lot, cancellationToken);

    return new LotDto(lot.Id, lot.Name, lot.Address, lot.Timezone, lot.AccessMode, lot.FullBehavior, lot.Status);
  }
}
