using Ardalis.GuardClauses;
using Parkin.Api.DriverFeatures.List;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Data.Queries;
using Parkin.Api.Infrastructure.Identity;
using Parkin.Api.LotFeatures.List;
using Parkin.Api.PlateFeatures.List;
using Parkin.Api.SpaceFeatures;
using Parkin.Api.SpaceFeatures.List;
using Parkin.Api.UserFeatures.List;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Parkin.Api.Infrastructure;
public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    // Always use PostgreSQL from Aspire
    string? connectionString = config.GetConnectionString("AppDb");
    Guard.Against.Null(connectionString, "AppDb connection string is required. Make sure the application is running with Aspire.");

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();
      
      options.UseNpgsql(connectionString);
      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddIdentityCore<ApplicationUser>(options =>
            {
              options.User.RequireUniqueEmail = true;
              options.Lockout.MaxFailedAccessAttempts = 5;
              options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
              options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
           .AddScoped<IListLotsQueryService, ListLotsQueryService>()
           .AddScoped<IListUsersQueryService, ListUsersQueryService>()
           .AddScoped<IListSpacesQueryService, ListSpacesQueryService>()
           .AddScoped<IListDriversQueryService, ListDriversQueryService>()
           .AddScoped<IListPlatesByDriverQueryService, ListPlatesByDriverQueryService>()
           .AddScoped<IActiveReservationChecker, NoActiveReservationChecker>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
