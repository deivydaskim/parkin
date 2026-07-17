namespace Parkin.Api.Domain.AuditAggregate;

public static class AuditActions
{
  public const string UserCreate = "user.create";
  public const string UserChangeRole = "user.change_role";
  public const string UserDisable = "user.disable";
  public const string UserEnable = "user.enable";
  public const string LotArchived = "lot.archived";
}
