using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Plates.Deactivate;

public record DeactivatePlateCommand(PlateId PlateId, Guid? ActorId) : ICommand<Result<PlateResponse>>;

public class DeactivatePlateHandler(IRepository<Driver> repository)
  : ICommandHandler<DeactivatePlateCommand, Result<PlateResponse>>
{
  public async ValueTask<Result<PlateResponse>> Handle(DeactivatePlateCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByPlateIdSpec(request.PlateId), cancellationToken);
    if (driver is null) return Result.NotFound();

    var result = driver.DeactivatePlate(request.PlateId, request.ActorId);
    if (result.IsSuccess) await repository.UpdateAsync(driver, cancellationToken);

    return result.Map(PlateResponse.From);
  }
}
