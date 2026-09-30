using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Auth;

public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles)
{
  public static CurrentUserResponse From(StaffUser user) => new(user.Id, user.Email, user.DisplayName, user.Roles);
}
