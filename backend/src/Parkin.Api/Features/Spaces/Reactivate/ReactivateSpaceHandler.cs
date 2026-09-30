using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Spaces.Reactivate;

public record ReactivateSpaceCommand(ParkingSpaceId SpaceId, Guid? ActorId) : ICommand<Result<SpaceResponse>>;

public class ReactivateSpaceHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<ReactivateSpaceCommand, Result<SpaceResponse>>
{
  public async ValueTask<Result<SpaceResponse>> Handle(ReactivateSpaceCommand request,
    CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotBySpaceIdSpec(request.SpaceId), cancellationToken);
    if (lot is null) return Result.NotFound();

    var result = lot.ReactivateSpace(request.SpaceId, request.ActorId);
    if (result.IsSuccess) await repository.UpdateAsync(lot, cancellationToken);

    return result.Map(SpaceResponse.From);
  }
}
