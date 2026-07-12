namespace Parkin.Api.UserFeatures.List;

public record ListUsersQuery(int? Page = 1,
  int? PerPage = Constants.DEFAULT_PAGE_SIZE)
  : IQuery<Result<PagedResult<UserRecord>>>;

public class ListUsersHandler(IListUsersQueryService query) : IQueryHandler<ListUsersQuery, Result<PagedResult<UserRecord>>>
{
  private readonly IListUsersQueryService _query = query;

  public async ValueTask<Result<PagedResult<UserRecord>>> Handle(ListUsersQuery request,
                                                                   CancellationToken cancellationToken)
  {
    var result = await _query.ListAsync(request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE);

    return Result.Success(result);
  }
}
