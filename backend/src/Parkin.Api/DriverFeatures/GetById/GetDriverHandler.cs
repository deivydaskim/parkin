using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;

namespace Parkin.Api.DriverFeatures.GetById;

public record GetDriverQuery(DriverId DriverId) : IQuery<Result<DriverDto>>;

public class GetDriverHandler(IReadRepository<Driver> repository)
  : IQueryHandler<GetDriverQuery, Result<DriverDto>>
{
  public async ValueTask<Result<DriverDto>> Handle(GetDriverQuery request, CancellationToken cancellationToken)
  {
    var spec = new DriverByIdSpec(request.DriverId);
    var entity = await repository.FirstOrDefaultAsync(spec, cancellationToken);
    if (entity == null) return Result.NotFound();

    return new DriverDto(entity.Id, entity.Name, entity.Contact, entity.Status, entity.Plates.Count);
  }
}
