namespace Parkin.Api.UserFeatures.List;

/// <summary>
/// Represents a service that will actually fetch the necessary data
/// Typically implemented in Infrastructure
/// </summary>
public interface IListUsersQueryService
{
  Task<PagedResult<UserRecord>> ListAsync(int page, int perPage);
}
