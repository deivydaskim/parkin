namespace Parkin.Api.Domain.StaffUsers;

public static class UserGuardrails
{
  public static bool WouldRemoveLastActiveSystemAdmin(int activeSystemAdminCount, bool targetIsActiveSystemAdmin)
    => targetIsActiveSystemAdmin && activeSystemAdminCount <= 1;

  public static async Task<bool> IsLastActiveSystemAdminAsync(this IStaffUserService staffUsers, StaffUser target,
    CancellationToken cancellationToken)
    => target.IsActiveSystemAdmin && WouldRemoveLastActiveSystemAdmin(
      await staffUsers.CountActiveSystemAdminsAsync(cancellationToken), targetIsActiveSystemAdmin: true);
}
