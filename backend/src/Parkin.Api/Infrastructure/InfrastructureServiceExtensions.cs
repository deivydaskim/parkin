using Ardalis.GuardClauses;
using Parkin.Api.Features.AccessEvents.List;
using Parkin.Api.Features.ApiKeys.List;
using Parkin.Api.Features.Audit.List;
using Parkin.Api.Features.Drivers.List;
using Parkin.Api.Features.Grants.List;
using Parkin.Api.Features.Grants.ListByLot;
using Parkin.Api.Features.Sessions.ListActiveByLot;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.Services;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Data.Queries;
using Parkin.Api.Infrastructure.Identity;
using Parkin.Api.Features.Lots.List;
using Parkin.Api.Features.LotLayouts.Get;
using Parkin.Api.Features.Occupancy.ListLotOccupancy;
using Parkin.Api.Features.Plates.List;
using Parkin.Api.Features.Spaces;
using Parkin.Api.Features.Spaces.List;
using Parkin.Api.Features.Users.List;
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
           .AddScoped<IListGrantsByDriverQueryService, ListGrantsByDriverQueryService>()
           .AddScoped<IListApiKeysQueryService, ListApiKeysQueryService>()
           .AddScoped<IListAuditQueryService, ListAuditQueryService>()
           .AddScoped<ILotLayoutQueryService, LotLayoutQueryService>()
           .AddScoped<IActiveLotOccupancyQueryService, ActiveLotOccupancyQueryService>()
           .AddScoped<IListAccessEventsQueryService, ListAccessEventsQueryService>()
           .AddScoped<IListActiveSessionsByLotQueryService, ListActiveSessionsByLotQueryService>()
           .AddScoped<IListGrantsByLotQueryService, ListGrantsByLotQueryService>()
           .AddScoped<IActiveReservationChecker, ActiveReservationChecker>()
           .AddScoped<ILotRowLocker, LotRowLocker>()
           .AddScoped<IUnitOfWork, EfUnitOfWork>();

    services.AddSingleton<IEntryDecisionService, EntryDecisionService>()
            .AddSingleton<IOccupancyCalculator, OccupancyCalculator>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
