using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.DriverFeatures.Create;

public record CreateDriverCommand(string Name, string? Contact, Guid? ActorId) : ICommand<Result<DriverDto>>;

public class CreateDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<CreateDriverCommand, Result<DriverDto>>
{
  public async ValueTask<Result<DriverDto>> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = Driver.Create(request.Name, request.Contact, request.ActorId);
    await repository.AddAsync(driver, cancellationToken);

    return new DriverDto(driver.Id, driver.Name, driver.Contact, driver.Status, driver.Plates.Count);
  }
}
