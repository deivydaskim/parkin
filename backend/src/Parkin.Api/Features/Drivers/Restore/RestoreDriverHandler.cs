using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Drivers.Restore;

public record RestoreDriverCommand(DriverId DriverId, Guid? ActorId) : ICommand<Result<DriverResponse>>;

public class RestoreDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<RestoreDriverCommand, Result<DriverResponse>>
{
  public async ValueTask<Result<DriverResponse>> Handle(RestoreDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver is null) return Result.NotFound();

    driver.Restore(request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return DriverResponse.From(driver);
  }
}
