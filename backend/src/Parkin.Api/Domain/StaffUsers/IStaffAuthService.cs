namespace Parkin.Api.Domain.StaffUsers;

public interface IStaffAuthService
{
  Task<StaffSignInResult> PasswordSignInAsync(Guid userId, string password, CancellationToken cancellationToken);

  Task SignOutAsync();
}
