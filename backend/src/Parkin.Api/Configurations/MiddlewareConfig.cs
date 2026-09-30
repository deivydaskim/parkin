using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Identity;
using Scalar.AspNetCore;

namespace Parkin.Api.Configurations;

public static class MiddlewareConfig
{
  public static WebApplication UseAppMiddleware(this WebApplication app)
  {
    app.UseExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
      app.UseHsts();
      app.UseHttpsRedirection();
    }

    app.UseCors(AuthConfig.CorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseFastEndpoints(c =>
      c.Serializer.Options.Converters.Add(new JsonStringEnumConverter()));

    if (app.Environment.IsDevelopment())
    {
      app.UseSwaggerGen(options => options.Path = "/openapi/{documentName}.json");
      app.MapScalarApiReference();
    }

    return app;
  }

  public static async Task MigrateAndSeedDatabaseAsync(this WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();
    var databaseOptions = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

    if (app.Environment.IsDevelopment() && databaseOptions.RecreateOnStartup)
    {
      logger.LogWarning("Dropping database for a fresh start (DatabaseOptions:RecreateOnStartup = true)");
      await context.Database.EnsureDeletedAsync();
    }

    logger.LogInformation("Applying database migrations");
    await context.Database.MigrateAsync();

    await SeedData.SeedIdentityAsync(
      services.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
      services.GetRequiredService<UserManager<ApplicationUser>>(),
      services.GetRequiredService<IOptions<SeedAdminOptions>>().Value,
      services.GetRequiredService<IOptions<SeedOperatorOptions>>().Value,
      logger);

    if (!databaseOptions.SeedDemoData) return;

    try
    {
      await SeedData.SeedDemoLotAsync(context, logger);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Seeding demo data failed; continuing without it");
    }
  }
}
