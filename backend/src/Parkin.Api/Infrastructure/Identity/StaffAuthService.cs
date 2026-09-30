using Microsoft.AspNetCore.Identity;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Infrastructure.Identity;

public sealed class StaffAuthService(SignInManager<ApplicationUser> signInManager) : IStaffAuthService
{
  public async Task<StaffSignInResult> PasswordSignInAsync(Guid userId, string password,
    CancellationToken cancellationToken)
  {
    var user = await signInManager.UserManager.FindByIdAsync(userId.ToString());
    if (user is null) return StaffSignInResult.InvalidCredentials;

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);
    if (result.Succeeded) return StaffSignInResult.Succeeded;
    return result.IsLockedOut ? StaffSignInResult.LockedOut : StaffSignInResult.InvalidCredentials;
  }

  public Task SignOutAsync() => signInManager.SignOutAsync();
}
