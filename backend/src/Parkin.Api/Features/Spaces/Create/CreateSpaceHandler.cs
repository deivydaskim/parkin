using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Spaces.Create;

public record CreateSpaceCommand(
  ParkingLotId LotId,
  string Label,
  SpaceType Type,
  Guid? ActorId,
  string? Zone = null,
  SpacePlacement? Placement = null) : ICommand<Result<SpaceResponse>>;

public class CreateSpaceHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<CreateSpaceCommand, Result<SpaceResponse>>
{
  public async ValueTask<Result<SpaceResponse>> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null) return Result.NotFound();

    var result = lot.AddSpace(request.Label, request.Type, request.ActorId, request.Zone, request.Placement);
    if (result.IsSuccess) await repository.UpdateAsync(lot, cancellationToken);

    return result.Map(SpaceResponse.From);
  }
}
