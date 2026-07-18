using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.SpaceFeatures.Update;

public record UpdateSpaceCommand(ParkingSpaceId SpaceId, string? Label, SpaceType? Type, Guid? ActorId) : ICommand<Result<SpaceDto>>;

public class UpdateSpaceHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<UpdateSpaceCommand, Result<SpaceDto>>
{
  public async ValueTask<Result<SpaceDto>> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot == null) return Result.NotFound();

    if (!string.IsNullOrWhiteSpace(request.Label))
    {
      var duplicate = lot.Spaces.FirstOrDefault(s => s.Label == request.Label && s.Id != request.SpaceId);
      if (duplicate != null)
      {
        return Result.Invalid(new ValidationError("Label", "A space with this label already exists in this lot"));
      }
    }

    lot.UpdateSpace(request.SpaceId, request.Label, request.Type, request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    var space = lot.Spaces.First(s => s.Id == request.SpaceId);
    return new SpaceDto(space.Id, space.LotId, space.Label, space.Type, space.Status);
  }
}
