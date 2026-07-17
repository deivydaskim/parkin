using Ardalis.ListStartupServices;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Identity;
using Scalar.AspNetCore;

namespace Parkin.Api.Configurations;

public static class MiddlewareConfig
{
  public static async Task<IApplicationBuilder> UseAppMiddlewareAndSeedDatabase(this WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseShowAllServicesMiddleware(); // see https://github.com/ardalis/AspNetCoreStartupServices
    }
    else
    {   
      app.UseDefaultExceptionHandler(); // from FastEndpoints
      app.UseHsts();
    }

    app.UseCors(AuthConfig.CorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseFastEndpoints(c =>
      c.Serializer.Options.Converters.Add(new JsonStringEnumConverter()));

    if (app.Environment.IsDevelopment())
    {
      app.UseSwaggerGen(options =>
      {
        options.Path = "/openapi/{documentName}.json";
      });
      app.MapScalarApiReference();
    }

    // In dev the SPA talks to the API over http (same scheme as the http page) so the
    // auth cookie survives; redirecting to https would flip the scheme and drop it.
    if (!app.Environment.IsDevelopment())
    {
      app.UseHttpsRedirection(); // Note this will drop Authorization headers
    }

    await SeedDatabase(app);

    return app;
  }

  static async Task SeedDatabase(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
      var context = services.GetRequiredService<AppDbContext>();
      var dbOptions = services.GetService<IOptions<DatabaseOptions>>()?.Value;
      
      // Drop and recreate database in development if configured
      if (app.Environment.IsDevelopment() && dbOptions?.RecreateOnStartup == true)
      {
        logger.LogWarning("DROPPING database for fresh start (DatabaseOptions:RecreateOnStartup = true)...");
        await context.Database.EnsureDeletedAsync();
        logger.LogInformation("Database dropped.");
      }

      // Apply all pending migrations
      logger.LogInformation("Applying database migrations...");
      await context.Database.MigrateAsync();
      logger.LogInformation("Database migrations applied successfully.");

      var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
      var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
      var seedAdmin = services.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
      var seedOperator = services.GetService<IOptions<SeedOperatorOptions>>()?.Value;
      await SeedData.SeedIdentityAsync(roleManager, userManager, seedAdmin, seedOperator, logger);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
    }
  }
}
