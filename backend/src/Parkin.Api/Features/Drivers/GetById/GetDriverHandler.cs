using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.Features.Drivers.GetById;

public record GetDriverQuery(DriverId DriverId) : IQuery<Result<DriverResponse>>;

public class GetDriverHandler(IReadRepository<Driver> repository)
  : IQueryHandler<GetDriverQuery, Result<DriverResponse>>
{
  public async ValueTask<Result<DriverResponse>> Handle(GetDriverQuery request, CancellationToken cancellationToken)
  {
    var driver = await repository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver is null) return Result.NotFound();

    return DriverResponse.From(driver);
  }
}
