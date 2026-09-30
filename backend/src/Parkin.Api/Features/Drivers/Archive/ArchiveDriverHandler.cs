using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Drivers.Archive;

public record ArchiveDriverCommand(DriverId DriverId, Guid? ActorId) : ICommand<Result<DriverResponse>>;

public class ArchiveDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<ArchiveDriverCommand, Result<DriverResponse>>
{
  public async ValueTask<Result<DriverResponse>> Handle(ArchiveDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver is null) return Result.NotFound();

    driver.Archive(request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return DriverResponse.From(driver);
  }
}
