using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.PlateFeatures.Reactivate;

public record ReactivatePlateCommand(PlateId PlateId, Guid? ActorId) : ICommand<Result<PlateDto>>;

public class ReactivatePlateHandler(IRepository<Driver> repository)
  : ICommandHandler<ReactivatePlateCommand, Result<PlateDto>>
{
  public async ValueTask<Result<PlateDto>> Handle(ReactivatePlateCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByPlateIdSpec(request.PlateId), cancellationToken);
    if (driver == null) return Result.NotFound();

    var plate = driver.Plates.First(p => p.Id == request.PlateId);

    var duplicate = await repository.FirstOrDefaultAsync(
      new PlateByNormalizedValueSpec(plate.NormalizedPlateNumber), cancellationToken);
    if (duplicate != null && duplicate.Id != driver.Id)
    {
      return Result.Invalid(new ValidationError("PlateId", "A plate with this number already exists"));
    }

    driver.ReactivatePlate(request.PlateId, request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return new PlateDto(plate.Id, plate.DriverId, plate.NormalizedPlateNumber, plate.Status);
  }
}
