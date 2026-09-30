using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Spaces.Deactivate;

public record DeactivateSpaceCommand(ParkingSpaceId SpaceId, Guid? ActorId) : ICommand<Result<SpaceResponse>>;

public class DeactivateSpaceHandler(IRepository<ParkingLot> repository, IActiveReservationChecker checker)
  : ICommandHandler<DeactivateSpaceCommand, Result<SpaceResponse>>
{
  public async ValueTask<Result<SpaceResponse>> Handle(DeactivateSpaceCommand request,
    CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot is null) return Result.NotFound();

    if (lot.FindSpace(request.SpaceId) is { Type: SpaceType.Reserved }
      && await checker.HasActiveReservationAsync(request.SpaceId, cancellationToken))
    {
      return Result.Invalid(new ValidationError("SpaceId", "Space has an active reservation and cannot be deactivated"));
    }

    var result = lot.DeactivateSpace(request.SpaceId, request.ActorId);
    if (result.IsSuccess) await repository.UpdateAsync(lot, cancellationToken);

    return result.Map(SpaceResponse.From);
  }
}
