using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Plates.List;

public record ListPlatesByDriverQuery(DriverId DriverId, int Page, int PerPage)
  : IQuery<Result<PagedResult<PlateResponse>>>;

public class ListPlatesByDriverHandler(IListPlatesByDriverQueryService query)
  : IQueryHandler<ListPlatesByDriverQuery, Result<PagedResult<PlateResponse>>>
{
  public async ValueTask<Result<PagedResult<PlateResponse>>> Handle(ListPlatesByDriverQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request.DriverId, request.Page, request.PerPage, cancellationToken);
}
