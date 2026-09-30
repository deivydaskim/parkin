namespace Parkin.Api.Features.Users.List;

public record ListUsersQuery(int Page, int PerPage) : IQuery<Result<PagedResult<UserResponse>>>;

public class ListUsersHandler(IListUsersQueryService query)
  : IQueryHandler<ListUsersQuery, Result<PagedResult<UserResponse>>>
{
  public async ValueTask<Result<PagedResult<UserResponse>>> Handle(ListUsersQuery request,
    CancellationToken cancellationToken)
    => await query.ListAsync(request.Page, request.PerPage, cancellationToken);
}
