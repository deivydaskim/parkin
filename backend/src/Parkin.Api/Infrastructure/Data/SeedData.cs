using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.ProductAggregate;
using Parkin.Api.Infrastructure.Identity;
using Microsoft.Extensions.Logging;

namespace Parkin.Api.Infrastructure.Data;

public static class SeedData
{
  public const int NUMBER_OF_PRODUCTS = 10;

  public static async Task InitializeAsync(AppDbContext dbContext, ILogger logger)
  {
    if (await dbContext.Products.AnyAsync())
    {
      logger.LogInformation("DB has data - seeding not required.");
      return; // DB has been seeded
    }
    await PopulateTestDataAsync(dbContext, logger);
  }

  public static async Task PopulateTestDataAsync(AppDbContext dbContext, ILogger logger)
  {
    logger.LogInformation("Seeding database with sample data.");

    // add more products to support demonstrating paging
    for (int i = 1; i <= NUMBER_OF_PRODUCTS; i++)
    {
      dbContext.Products.Add(new Product(ProductId.From(i), $"Product {i}", 10m + i));
    }
    await dbContext.SaveChangesAsync();
  }

  public static async Task SeedIdentityAsync(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    SeedAdminOptions admin,
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
  }

  static string DescribeErrors(IdentityResult result) =>
    string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
