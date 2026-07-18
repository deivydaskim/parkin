using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.DriverFeatures.Update;

public record UpdateDriverCommand(
  DriverId DriverId,
  string? Name,
  string? Contact,
  Guid? ActorId) : ICommand<Result<DriverDto>>;

public class UpdateDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<UpdateDriverCommand, Result<DriverDto>>
{
  public async ValueTask<Result<DriverDto>> Handle(UpdateDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver == null) return Result.NotFound();

    var name = request.Name ?? driver.Name;
    var contact = request.Contact ?? driver.Contact;
    driver.UpdateDetails(name, contact, request.ActorId);

    await repository.UpdateAsync(driver, cancellationToken);

    return new DriverDto(driver.Id, driver.Name, driver.Contact, driver.Status, driver.Plates.Count);
  }
}
