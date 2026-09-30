using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Spaces.List;

public record ListSpacesQuery(
  ParkingLotId LotId,
  int Page,
  int PerPage,
  SpaceStatusFilter? Status = null,
  SpaceType? Type = null,
  string? Search = null)
  : IQuery<Result<PagedResult<SpaceResponse>>>;

public class ListSpacesHandler(IListSpacesQueryService query)
  : IQueryHandler<ListSpacesQuery, Result<PagedResult<SpaceResponse>>>
{
  public async ValueTask<Result<PagedResult<SpaceResponse>>> Handle(ListSpacesQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request, cancellationToken);
}
