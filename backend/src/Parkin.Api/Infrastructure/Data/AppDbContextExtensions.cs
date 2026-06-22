using Microsoft.EntityFrameworkCore;

namespace Parkin.Api.Infrastructure.Data;

public static class AppDbContextExtensions
{
  public static void AddApplicationDbContext(this IServiceCollection services, string connectionString) =>
    services.AddDbContext<AppDbContext>(options =>
         options.UseNpgsql(connectionString));

}
