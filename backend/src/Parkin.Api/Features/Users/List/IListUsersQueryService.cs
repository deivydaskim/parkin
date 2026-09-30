namespace Parkin.Api.Features.Users.List;

public interface IListUsersQueryService
{
  Task<PagedResult<UserResponse>> ListAsync(int page, int perPage, CancellationToken cancellationToken);
}
