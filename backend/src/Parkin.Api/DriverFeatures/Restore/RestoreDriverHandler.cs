using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.DriverFeatures.Restore;

public record RestoreDriverCommand(DriverId DriverId, Guid? ActorId) : ICommand<Result<DriverDto>>;

public class RestoreDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<RestoreDriverCommand, Result<DriverDto>>
{
  public async ValueTask<Result<DriverDto>> Handle(RestoreDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver == null) return Result.NotFound();

    driver.Restore(request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return new DriverDto(driver.Id, driver.Name, driver.Contact, driver.Status, driver.Plates.Count);
  }
}
