using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.LotFeatures.Create;

public record CreateLotCommand(
  string Name,
  string? Address,
  string Timezone,
  AccessMode AccessMode,
  FullBehavior FullBehavior,
  Guid? ActorId) : ICommand<Result<LotDto>>;

public class CreateLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<CreateLotCommand, Result<LotDto>>
{
  public async ValueTask<Result<LotDto>> Handle(CreateLotCommand request, CancellationToken cancellationToken)
  {
    var existing = await repository.FirstOrDefaultAsync(new ParkingLotByNameSpec(request.Name), cancellationToken);
    if (existing != null)
    {
      return Result.Invalid(new ValidationError("Name", "A lot with this name already exists"));
    }

    var lot = ParkingLot.Create(request.Name, request.Timezone, request.Address, request.AccessMode, request.FullBehavior, request.ActorId);
    await repository.AddAsync(lot, cancellationToken);

    return new LotDto(lot.Id, lot.Name, lot.Address, lot.Timezone, lot.AccessMode, lot.FullBehavior, lot.Status, lot.Capacity);
  }
}
