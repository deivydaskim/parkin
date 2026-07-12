namespace Parkin.Api.Authorization;

public static class AccessPolicies
{
  public static readonly string[] OperatorOrAbove = ["Operator", "SystemAdmin"];
  public static readonly string[] AdminOnly = ["SystemAdmin"];
}
