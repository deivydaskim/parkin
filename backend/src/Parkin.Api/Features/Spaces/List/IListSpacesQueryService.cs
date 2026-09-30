namespace Parkin.Api.Features.Spaces.List;

public interface IListSpacesQueryService
{
  Task<PagedResult<SpaceResponse>> ListAsync(ListSpacesQuery query, CancellationToken cancellationToken);
}
