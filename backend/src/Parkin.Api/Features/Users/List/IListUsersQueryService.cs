namespace Parkin.Api.Features.Users.List;

public interface IListUsersQueryService
{
  Task<PagedResult<UserRecord>> ListAsync(int page, int perPage);
}
