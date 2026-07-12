namespace Parkin.Api.Infrastructure.Identity;

public static class Roles
{
  public const string SystemAdmin = "SystemAdmin";
  public const string Operator = "Operator";

  public static readonly string[] All = [SystemAdmin, Operator];
}
