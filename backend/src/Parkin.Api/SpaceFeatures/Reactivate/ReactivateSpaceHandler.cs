using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.SpaceFeatures.Reactivate;

public record ReactivateSpaceCommand(ParkingSpaceId SpaceId, Guid? ActorId) : ICommand<Result<SpaceDto>>;

public class ReactivateSpaceHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<ReactivateSpaceCommand, Result<SpaceDto>>
{
  public async ValueTask<Result<SpaceDto>> Handle(ReactivateSpaceCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot == null) return Result.NotFound();

    var space = lot.Spaces.First(s => s.Id == request.SpaceId);

    var duplicate = lot.Spaces.FirstOrDefault(s => s.Label == space.Label && s.Id != space.Id);
    if (duplicate != null)
    {
      return Result.Invalid(new ValidationError("Label", "A space with this label already exists in this lot"));
    }

    lot.ReactivateSpace(request.SpaceId, request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    return new SpaceDto(space.Id, space.LotId, space.Label, space.Type, space.Status);
  }
}
