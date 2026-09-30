using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Lots.Create;

public record CreateLotCommand(
  string Name,
  string? Address,
  string Timezone,
  AccessMode AccessMode,
  FullBehavior FullBehavior,
  Guid? ActorId,
  LotLayout? Layout = null) : ICommand<Result<LotResponse>>;

public class CreateLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<CreateLotCommand, Result<LotResponse>>
{
  public async ValueTask<Result<LotResponse>> Handle(CreateLotCommand request, CancellationToken cancellationToken)
  {
    if (await repository.AnyAsync(new ParkingLotByNameSpec(request.Name), cancellationToken))
    {
      return Result.Invalid(LotErrors.DuplicateName);
    }

    var lot = ParkingLot.Create(request.Name, request.Timezone, request.Address, request.AccessMode,
      request.FullBehavior, request.ActorId, request.Layout);
    await repository.AddAsync(lot, cancellationToken);

    return LotResponse.From(lot);
  }
}
