using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Parkin.Api.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating DbContext instances for EF Core tools.
/// This is ONLY used when CREATING migrations (e.g., 'dotnet ef migrations add').
/// It is NOT used at runtime - Aspire handles the runtime connection.
/// 
/// Migrations are automatically APPLIED at runtime via MigrateAsync() in MiddlewareConfig.cs.
///
/// To create migrations with PostgreSQL, set an environment variable:
/// PowerShell: $env:ConnectionStrings__AppDb = "Host=localhost;Port=5432;Database=AppDb;Username=postgres;Password=YourPassword"
/// Bash: export ConnectionStrings__AppDb="Host=localhost;Port=5432;Database=AppDb;Username=postgres;Password=YourPassword"
///
/// Always uses PostgreSQL for migrations.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
  public AppDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

    // Try to get connection string from environment variable (ASP.NET Core format)
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AppDb")
                          ?? "Host=localhost;Port=5432;Database=AppDb_Design;Username=postgres;Password=postgres"; // Default PostgreSQL connection for design-time

    optionsBuilder.UseNpgsql(connectionString);

    return new AppDbContext(optionsBuilder.Options);
  }
}
