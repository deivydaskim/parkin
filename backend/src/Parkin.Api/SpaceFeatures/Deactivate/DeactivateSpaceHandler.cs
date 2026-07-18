using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.SpaceFeatures.Deactivate;

public record DeactivateSpaceCommand(ParkingSpaceId SpaceId, Guid? ActorId) : ICommand<Result<SpaceDto>>;

public class DeactivateSpaceHandler(IRepository<ParkingLot> repository, IActiveReservationChecker checker)
  : ICommandHandler<DeactivateSpaceCommand, Result<SpaceDto>>
{
  public async ValueTask<Result<SpaceDto>> Handle(DeactivateSpaceCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot == null) return Result.NotFound();

    var space = lot.Spaces.First(s => s.Id == request.SpaceId);

    if (space.Type == SpaceType.Reserved && await checker.HasActiveReservationAsync(request.SpaceId, cancellationToken))
    {
      return Result.Invalid(new ValidationError("SpaceId", "Space has an active reservation and cannot be deactivated"));
    }

    lot.DeactivateSpace(request.SpaceId, request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    return new SpaceDto(space.Id, space.LotId, space.Label, space.Type, space.Status);
  }
}
