using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Users;

public sealed record UserResponse(Guid Id, string Email, string DisplayName, string Role, string Status)
{
  public static UserResponse From(StaffUser user)
    => new(user.Id, user.Email, user.DisplayName, user.Role, user.Status.ToString());
}
