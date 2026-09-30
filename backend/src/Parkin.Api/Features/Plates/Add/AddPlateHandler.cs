using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Plates.Add;

public record AddPlateCommand(DriverId DriverId, string RawPlate, Guid? ActorId) : ICommand<Result<PlateResponse>>;

public class AddPlateHandler(IRepository<Driver> repository)
  : ICommandHandler<AddPlateCommand, Result<PlateResponse>>
{
  public async ValueTask<Result<PlateResponse>> Handle(AddPlateCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver is null) return Result.NotFound();

    var normalized = PlateNormalizer.Normalize(request.RawPlate);
    if (await repository.AnyAsync(new PlateByNormalizedValueSpec(normalized), cancellationToken))
    {
      return Result.Invalid(new ValidationError("PlateNumber", "A driver with this plate already exists"));
    }

    var plate = driver.AddPlate(request.RawPlate, request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return PlateResponse.From(plate);
  }
}
