using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Spaces.Update;

public record UpdateSpaceCommand(ParkingSpaceId SpaceId, SpaceUpdate Update, Guid? ActorId)
  : ICommand<Result<SpaceResponse>>;

public class UpdateSpaceHandler(IRepository<ParkingLot> repository, IActiveReservationChecker checker)
  : ICommandHandler<UpdateSpaceCommand, Result<SpaceResponse>>
{
  public async ValueTask<Result<SpaceResponse>> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot is null) return Result.NotFound();

    if (await ChangesTypeOfReservedSpaceAsync(lot, request, cancellationToken))
    {
      return Result.Invalid(new ValidationError("Type", "Space has an active reservation and its type cannot be changed"));
    }

    var result = lot.UpdateSpace(request.SpaceId, request.Update, request.ActorId);
    if (result.IsSuccess) await repository.UpdateAsync(lot, cancellationToken);

    return result.Map(SpaceResponse.From);
  }

  private async Task<bool> ChangesTypeOfReservedSpaceAsync(ParkingLot lot, UpdateSpaceCommand request,
    CancellationToken cancellationToken)
  {
    var space = lot.FindSpace(request.SpaceId);
    return request.Update.Type is { } type
      && space is not null
      && space.Type != type
      && await checker.HasActiveReservationAsync(request.SpaceId, cancellationToken);
  }
}
