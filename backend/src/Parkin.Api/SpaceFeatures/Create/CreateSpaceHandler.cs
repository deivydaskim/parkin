using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.SpaceFeatures.Create;

public record CreateSpaceCommand(
  ParkingLotId LotId,
  string Label,
  SpaceType Type,
  Guid? ActorId,
  string? Zone = null,
  SpacePlacement? Placement = null) : ICommand<Result<SpaceDto>>;

public class CreateSpaceHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<CreateSpaceCommand, Result<SpaceDto>>
{
  public async ValueTask<Result<SpaceDto>> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot == null) return Result.NotFound();

    if (lot.Spaces.Any(s => s.Label == request.Label))
    {
      return Result.Invalid(new ValidationError("Label", "A space with this label already exists in this lot"));
    }

    if (request.Placement is not null)
    {
      var placementCheck = lot.CheckPlacement(request.Placement);
      if (!placementCheck.IsSuccess) return Result.Invalid(placementCheck.ValidationErrors);
    }

    var space = lot.AddSpace(request.Label, request.Type, request.ActorId, request.Zone, request.Placement);
    await repository.UpdateAsync(lot, cancellationToken);

    return SpaceDto.FromEntity(space);
  }
}
