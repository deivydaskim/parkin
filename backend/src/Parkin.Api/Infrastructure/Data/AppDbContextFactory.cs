using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Parkin.Api.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
  private const string DefaultDesignTimeConnectionString =
    "Host=localhost;Port=5432;Database=AppDb_Design;Username=postgres;Password=postgres";

  public AppDbContext CreateDbContext(string[] args)
  {
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AppDb")
      ?? DefaultDesignTimeConnectionString;

    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(connectionString)
      .Options;

    return new AppDbContext(options);
  }
}
