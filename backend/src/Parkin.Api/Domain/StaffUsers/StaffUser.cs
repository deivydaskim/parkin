namespace Parkin.Api.Domain.StaffUsers;

public sealed record StaffUser(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles, UserStatus Status)
{
  public string Role => Roles.FirstOrDefault() ?? string.Empty;

  public bool IsSystemAdmin => Roles.Contains(StaffRoles.SystemAdmin);

  public bool IsActiveSystemAdmin => Status == UserStatus.Active && IsSystemAdmin;
}
