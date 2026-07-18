namespace Parkin.Api.Domain.AuditAggregate;

public static class AuditActions
{
  public const string UserCreate = "user.create";
  public const string UserChangeRole = "user.change_role";
  public const string UserDisable = "user.disable";
  public const string UserEnable = "user.enable";
  public const string LotCreated = "lot.created";
  public const string LotUpdated = "lot.updated";
  public const string LotArchived = "lot.archived";
  public const string LotRestored = "lot.restored";
  public const string SpaceCreated = "space.created";
  public const string SpaceUpdated = "space.updated";
  public const string SpaceDeactivated = "space.deactivated";
  public const string SpaceReactivated = "space.reactivated";
  public const string DriverCreated = "driver.created";
  public const string DriverUpdated = "driver.updated";
  public const string DriverArchived = "driver.archived";
  public const string DriverRestored = "driver.restored";
  public const string PlateAdded = "plate.added";
  public const string PlateReassigned = "plate.reassigned";
  public const string PlateDeactivated = "plate.deactivated";
  public const string PlateReactivated = "plate.reactivated";
  public const string GrantCreated = "grant.created";
  public const string GrantRevoked = "grant.revoked";
}
