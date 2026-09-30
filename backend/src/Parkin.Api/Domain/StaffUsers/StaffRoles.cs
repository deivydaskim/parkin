namespace Parkin.Api.Domain.StaffUsers;

public static class StaffRoles
{
  public const string SystemAdmin = "SystemAdmin";
  public const string Operator = "Operator";

  public static readonly string[] All = [SystemAdmin, Operator];
}
