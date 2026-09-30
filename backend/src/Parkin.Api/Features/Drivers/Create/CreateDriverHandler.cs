using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Drivers.Create;

public record CreateDriverCommand(string Name, string? Contact, Guid? ActorId) : ICommand<Result<DriverResponse>>;

public class CreateDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<CreateDriverCommand, Result<DriverResponse>>
{
  public async ValueTask<Result<DriverResponse>> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = Driver.Create(request.Name, request.Contact, request.ActorId);
    await repository.AddAsync(driver, cancellationToken);

    return DriverResponse.From(driver);
  }
}
