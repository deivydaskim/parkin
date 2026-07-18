using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.SpaceFeatures.Update;

public record UpdateSpaceCommand(ParkingSpaceId SpaceId, string? Label, SpaceType? Type, Guid? ActorId) : ICommand<Result<SpaceDto>>;

public class UpdateSpaceHandler(IRepository<ParkingLot> repository, IActiveReservationChecker checker)
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

    var space = lot.Spaces.First(s => s.Id == request.SpaceId);
    if (request.Type.HasValue && request.Type.Value != space.Type
      && await checker.HasActiveReservationAsync(request.SpaceId, cancellationToken))
    {
      return Result.Invalid(new ValidationError("Type", "Space has an active reservation and its type cannot be changed"));
    }

    lot.UpdateSpace(request.SpaceId, request.Label, request.Type, request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    return new SpaceDto(space.Id, space.LotId, space.Label, space.Type, space.Status);
  }
}
