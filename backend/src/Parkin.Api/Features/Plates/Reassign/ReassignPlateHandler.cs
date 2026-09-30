using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Plates.Reassign;

public record ReassignPlateCommand(PlateId PlateId, DriverId TargetDriverId, Guid? ActorId)
  : ICommand<Result<PlateResponse>>;

public class ReassignPlateHandler(IRepository<Driver> repository)
  : ICommandHandler<ReassignPlateCommand, Result<PlateResponse>>
{
  public async ValueTask<Result<PlateResponse>> Handle(ReassignPlateCommand request, CancellationToken cancellationToken)
  {
    var sourceDriver = await repository.FirstOrDefaultAsync(new DriverByPlateIdSpec(request.PlateId), cancellationToken);
    if (sourceDriver is null) return Result.NotFound();

    var targetDriver = sourceDriver.Id == request.TargetDriverId
      ? sourceDriver
      : await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.TargetDriverId), cancellationToken);
    if (targetDriver is null) return Result.NotFound();

    var result = sourceDriver.TransferPlate(request.PlateId, targetDriver, request.ActorId);
    if (result.IsSuccess) await repository.SaveChangesAsync(cancellationToken);

    return result.Map(PlateResponse.From);
  }
}
