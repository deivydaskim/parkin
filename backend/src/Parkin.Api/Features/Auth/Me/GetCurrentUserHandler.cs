using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Auth.Me;

public record GetCurrentUserQuery(Guid? UserId) : IQuery<Result<CurrentUserResponse>>;

public class GetCurrentUserHandler(IStaffUserService staffUsers)
  : IQueryHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
  public async ValueTask<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery query,
    CancellationToken cancellationToken)
  {
    if (query.UserId is not { } userId) return Result.Unauthorized();

    var user = await staffUsers.FindByIdAsync(userId, cancellationToken);
    if (user is null || user.Status == UserStatus.Disabled) return Result.Unauthorized();

    return CurrentUserResponse.From(user);
  }
}
