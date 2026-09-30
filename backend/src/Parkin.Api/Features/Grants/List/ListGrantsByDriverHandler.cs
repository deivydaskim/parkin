using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Features.Grants.List;

public record ListGrantsByDriverQuery(DriverId DriverId, int Page, int PerPage)
  : IQuery<Result<PagedResult<GrantResponse>>>;

public class ListGrantsByDriverHandler(IListGrantsByDriverQueryService query)
  : IQueryHandler<ListGrantsByDriverQuery, Result<PagedResult<GrantResponse>>>
{
  public async ValueTask<Result<PagedResult<GrantResponse>>> Handle(ListGrantsByDriverQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request.DriverId, request.Page, request.PerPage, cancellationToken);
}
