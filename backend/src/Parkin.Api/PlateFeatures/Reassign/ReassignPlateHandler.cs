using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.PlateFeatures.Reassign;

public record ReassignPlateCommand(PlateId PlateId, DriverId TargetDriverId, Guid? ActorId) : ICommand<Result<PlateDto>>;

public class ReassignPlateHandler(IRepository<Driver> repository)
  : ICommandHandler<ReassignPlateCommand, Result<PlateDto>>
{
  public async ValueTask<Result<PlateDto>> Handle(ReassignPlateCommand request, CancellationToken cancellationToken)
  {
    var sourceDriver = await repository.FirstOrDefaultAsync(new DriverByPlateIdSpec(request.PlateId), cancellationToken);
    if (sourceDriver == null) return Result.NotFound();

    if (sourceDriver.Id == request.TargetDriverId)
    {
      return Result.Invalid(new ValidationError("TargetDriverId", "Plate already belongs to this driver"));
    }

    var targetDriver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.TargetDriverId), cancellationToken);
    if (targetDriver == null) return Result.NotFound();

    var fromDriverId = sourceDriver.Id;
    var plate = sourceDriver.RemovePlateForReassignment(request.PlateId);
    targetDriver.ReceivePlate(plate, fromDriverId, request.ActorId);

    await repository.SaveChangesAsync(cancellationToken);

    return new PlateDto(plate.Id, plate.DriverId, plate.NormalizedPlateNumber, plate.Status);
  }
}
