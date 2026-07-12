using Microsoft.AspNetCore.Identity;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.AuthFeatures;

public sealed record CurrentUserResponse(
  Guid Id,
  string Email,
  string DisplayName,
  IList<string> Roles);

public static class CurrentUserResponseFactory
{
  public static async Task<CurrentUserResponse> BuildAsync(
    UserManager<ApplicationUser> userManager,
    ApplicationUser user)
  {
    var roles = await userManager.GetRolesAsync(user);
    return new CurrentUserResponse(user.Id, user.Email ?? string.Empty, user.DisplayName, roles);
  }
}
