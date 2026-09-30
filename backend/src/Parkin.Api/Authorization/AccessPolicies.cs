using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Authorization;

public static class AccessPolicies
{
  public static readonly string[] OperatorOrAbove = [StaffRoles.Operator, StaffRoles.SystemAdmin];
  public static readonly string[] AdminOnly = [StaffRoles.SystemAdmin];
}
