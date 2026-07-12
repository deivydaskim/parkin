using Microsoft.AspNetCore.Identity;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.UserFeatures;

public static class UserGuardrails
{
  public static bool WouldRemoveLastActiveSystemAdmin(int activeSystemAdminCount, bool targetIsActiveSystemAdmin)
    => targetIsActiveSystemAdmin && activeSystemAdminCount <= 1;

  public static async Task<bool> IsLastActiveSystemAdminAsync(
    UserManager<ApplicationUser> userManager, ApplicationUser target)
  {
    if (target.Status != UserStatus.Active) return false;

    var targetRoles = await userManager.GetRolesAsync(target);
    if (!targetRoles.Contains(Roles.SystemAdmin)) return false;

    var admins = await userManager.GetUsersInRoleAsync(Roles.SystemAdmin);
    var activeAdminCount = admins.Count(u => u.Status == UserStatus.Active);

    return WouldRemoveLastActiveSystemAdmin(activeAdminCount, targetIsActiveSystemAdmin: true);
  }
}
