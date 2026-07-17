namespace Parkin.Api.UserFeatures.List;

public interface IListUsersQueryService
{
  Task<PagedResult<UserRecord>> ListAsync(int page, int perPage);
}
