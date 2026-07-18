using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.PlateFeatures.Add;

public record AddPlateCommand(DriverId DriverId, string RawPlate, Guid? ActorId) : ICommand<Result<PlateDto>>;

public class AddPlateHandler(IRepository<Driver> repository)
  : ICommandHandler<AddPlateCommand, Result<PlateDto>>
{
  public async ValueTask<Result<PlateDto>> Handle(AddPlateCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver == null) return Result.NotFound();

    var normalized = PlateNormalizer.Normalize(request.RawPlate);
    var existing = await repository.FirstOrDefaultAsync(new PlateByNormalizedValueSpec(normalized), cancellationToken);
    if (existing != null)
    {
      return Result.Invalid(new ValidationError("PlateNumber", "A driver with this plate already exists"));
    }

    var plate = driver.AddPlate(request.RawPlate, request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return new PlateDto(plate.Id, plate.DriverId, plate.NormalizedPlateNumber, plate.Status);
  }
}
