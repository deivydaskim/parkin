using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Plates.Reactivate;

public record ReactivatePlateCommand(PlateId PlateId, Guid? ActorId) : ICommand<Result<PlateResponse>>;

public class ReactivatePlateHandler(IRepository<Driver> repository)
  : ICommandHandler<ReactivatePlateCommand, Result<PlateResponse>>
{
  public async ValueTask<Result<PlateResponse>> Handle(ReactivatePlateCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByPlateIdSpec(request.PlateId), cancellationToken);
    if (driver is null) return Result.NotFound();

    var result = driver.ReactivatePlate(request.PlateId, request.ActorId);
    if (result.IsSuccess) await repository.UpdateAsync(driver, cancellationToken);

    return result.Map(PlateResponse.From);
  }
}
