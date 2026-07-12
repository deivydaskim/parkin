using Microsoft.AspNetCore.Identity;
using Parkin.Api.Infrastructure.Identity;
using Microsoft.Extensions.Logging;

namespace Parkin.Api.Infrastructure.Data;

public static class SeedData
{
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
