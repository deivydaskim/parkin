using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.DriverFeatures.Archive;

public record ArchiveDriverCommand(DriverId DriverId, Guid? ActorId) : ICommand<Result<DriverDto>>;

public class ArchiveDriverHandler(IRepository<Driver> repository)
  : ICommandHandler<ArchiveDriverCommand, Result<DriverDto>>
{
  public async ValueTask<Result<DriverDto>> Handle(ArchiveDriverCommand request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver == null) return Result.NotFound();

    driver.Archive(request.ActorId);
    await repository.UpdateAsync(driver, cancellationToken);

    return new DriverDto(driver.Id, driver.Name, driver.Contact, driver.Status, driver.Plates.Count);
  }
}
