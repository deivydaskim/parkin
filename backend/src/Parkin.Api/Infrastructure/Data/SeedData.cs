using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ReservationAggregate;
using Parkin.Api.Infrastructure.Identity;
using Microsoft.Extensions.Logging;

namespace Parkin.Api.Infrastructure.Data;

public static class SeedData
{
  public const string DemoLotName = "Demo Parking - Central";

  private const decimal BayWidth = 2.5m;
  private const decimal BayLength = 5m;
  private const decimal RowStartX = 4m;

  private sealed record DemoRow(string Prefix, int Count, decimal CenterY, decimal Rotation, int Level,
    string Zone, int ReservedFrom = int.MaxValue);

  private static readonly DemoRow[] DemoRows =
  [
    new("A", 14, 3.5m, 0m, 0, "North", ReservedFrom: 13),
    new("B", 14, 15m, 180m, 0, "North"),
    new("C", 14, 20m, 0m, 0, "South"),
    new("D", 10, 31.5m, 180m, 0, "South", ReservedFrom: 1),
    new("L1-A", 12, 3.5m, 0m, 1, "Upper deck"),
    new("L1-B", 12, 15m, 180m, 1, "Upper deck", ReservedFrom: 11),
  ];

  public static async Task SeedDemoLotAsync(AppDbContext context, ILogger logger)
  {
    if (await context.ParkingLots.AnyAsync(lot => lot.Name == DemoLotName))
    {
      logger.LogInformation("Demo lot {Name} already exists - skipping.", DemoLotName);
      return;
    }

    var layout = LotLayout.Create(48m, 34m, levelCount: 2).Value;
    var lot = ParkingLot.Create(DemoLotName, "Europe/Vilnius", "1 Demo Street", layout: layout);

    foreach (var row in DemoRows)
    {
      for (var index = 1; index <= row.Count; index++)
      {
        var x = RowStartX + BayWidth / 2 + (index - 1) * BayWidth;
        var placement = SpacePlacement.Create(x, row.CenterY, row.Rotation, row.Level, BayWidth, BayLength).Value;
        var type = index >= row.ReservedFrom ? SpaceType.Reserved : SpaceType.General;
        lot.AddSpace($"{row.Prefix}{index:00}", type, actorId: null, row.Zone, placement);
      }
    }

    lot.AddSpace("X01", SpaceType.General, actorId: null, "Overflow");
    lot.AddSpace("X02", SpaceType.General, actorId: null, "Overflow");

    var closedBay = lot.Spaces.First(space => space.Label == "B07");
    lot.DeactivateSpace(closedBay.Id, actorId: null);

    context.ParkingLots.Add(lot);

    var reservedSpaces = lot.Spaces
      .Where(space => space.Type == SpaceType.Reserved)
      .OrderBy(space => space.Label)
      .Take(3)
      .ToList();
    string[] driverNames = ["Ona Petraitė", "Jonas Kazlauskas", "Rūta Jankauskienė"];

    foreach (var (space, driverName) in reservedSpaces.Zip(driverNames))
    {
      var driver = Driver.Create(driverName, contact: null, actorId: null);
      context.Drivers.Add(driver);
      context.Reservations.Add(Reservation.Create(space.Id, driver.Id, lot.Id, actorId: null));
    }

    await context.SaveChangesAsync();
    logger.LogInformation("Seeded demo lot {Name} with {Count} spaces.", DemoLotName, lot.Spaces.Count);
  }

  public static async Task SeedIdentityAsync(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    SeedAdminOptions admin,
    SeedOperatorOptions? operatorOpts,
    ILogger logger)
  {
    foreach (var roleName in Roles.All)
    {
      if (!await roleManager.RoleExistsAsync(roleName))
      {
        var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        if (!roleResult.Succeeded)
        {
          logger.LogError("Failed to create role {Role}: {Errors}", roleName, DescribeErrors(roleResult));
          return;
        }
        logger.LogInformation("Seeded role {Role}.", roleName);
      }
    }

    if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
    {
      logger.LogWarning("SeedAdmin email/password not configured - skipping admin user seed.");
      return;
    }

    if (await userManager.FindByEmailAsync(admin.Email) is not null)
    {
      logger.LogInformation("Admin user {Email} already exists - skipping.", admin.Email);
      return;
    }

    var user = new ApplicationUser
    {
      UserName = admin.Email,
      Email = admin.Email,
      EmailConfirmed = true,
      DisplayName = "System Administrator",
      Status = UserStatus.Active
    };

    var createResult = await userManager.CreateAsync(user, admin.Password);
    if (!createResult.Succeeded)
    {
      logger.LogError("Failed to create admin user {Email}: {Errors}", admin.Email, DescribeErrors(createResult));
      return;
    }

    var addToRoleResult = await userManager.AddToRoleAsync(user, Roles.SystemAdmin);
    if (!addToRoleResult.Succeeded)
    {
      logger.LogError("Failed to add admin {Email} to {Role}: {Errors}", admin.Email, Roles.SystemAdmin, DescribeErrors(addToRoleResult));
      return;
    }

    logger.LogInformation("Seeded admin user {Email} in role {Role}.", admin.Email, Roles.SystemAdmin);

    if (operatorOpts is null || string.IsNullOrWhiteSpace(operatorOpts.Email) || string.IsNullOrWhiteSpace(operatorOpts.Password))
    {
      return;
    }

    if (await userManager.FindByEmailAsync(operatorOpts.Email) is not null)
    {
      logger.LogInformation("Operator user {Email} already exists - skipping.", operatorOpts.Email);
      return;
    }

    var operatorUser = new ApplicationUser
    {
      UserName = operatorOpts.Email,
      Email = operatorOpts.Email,
      EmailConfirmed = true,
      DisplayName = "Parking Operator",
      Status = UserStatus.Active
    };

    var operatorCreateResult = await userManager.CreateAsync(operatorUser, operatorOpts.Password);
    if (!operatorCreateResult.Succeeded)
    {
      logger.LogError("Failed to create operator user {Email}: {Errors}", operatorOpts.Email, DescribeErrors(operatorCreateResult));
      return;
    }

    var operatorAddToRoleResult = await userManager.AddToRoleAsync(operatorUser, Roles.Operator);
    if (!operatorAddToRoleResult.Succeeded)
    {
      logger.LogError("Failed to add operator {Email} to {Role}: {Errors}", operatorOpts.Email, Roles.Operator, DescribeErrors(operatorAddToRoleResult));
      return;
    }

    logger.LogInformation("Seeded operator user {Email} in role {Role}.", operatorOpts.Email, Roles.Operator);
  }

  static string DescribeErrors(IdentityResult result) =>
    string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
