using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.PlateFeatures.Deactivate;

public record DeactivatePlateCommand(PlateId PlateId, Guid? ActorId) : ICommand<Result<PlateDto>>;

public class DeactivatePlateHandler(IRepository<Driver> repository)
  : ICommandHandler<DeactivatePlateCommand, Result<PlateDto>>
{
  public async ValueTask<Result<PlateDto>> Handle(DeactivatePlateCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByPlateIdSpec(request.PlateId), cancellationToken);
    if (driver == null) return Result.NotFound();

    var plate = driver.Plates.First(p => p.Id == request.PlateId);

    driver.DeactivatePlate(request.PlateId, request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return new PlateDto(plate.Id, plate.DriverId, plate.NormalizedPlateNumber, plate.Status);
  }
}
