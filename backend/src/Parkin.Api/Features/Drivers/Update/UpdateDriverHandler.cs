using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Drivers.Update;

public record UpdateDriverCommand(
  DriverId DriverId,
  string? Name,
  string? Contact,
  Guid? ActorId) : ICommand<Result<DriverResponse>>;

public class UpdateDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<UpdateDriverCommand, Result<DriverResponse>>
{
  public async ValueTask<Result<DriverResponse>> Handle(UpdateDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver is null) return Result.NotFound();

    driver.UpdateDetails(request.Name ?? driver.Name, request.Contact ?? driver.Contact, request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return DriverResponse.From(driver);
  }
}
