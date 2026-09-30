using Ardalis.GuardClauses;
using Parkin.Api.Features.AccessEvents.Ingest;
using Parkin.Api.Features.AccessEvents.List;
using Parkin.Api.Features.ApiKeys.List;
using Parkin.Api.Features.Audit.List;
using Parkin.Api.Features.Drivers.List;
using Parkin.Api.Features.Grants.List;
using Parkin.Api.Features.Grants.ListByLot;
using Parkin.Api.Features.Sessions.ListActiveByLot;
using Parkin.Api.Domain.Interfaces;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Data.Queries;
using Parkin.Api.Infrastructure.Identity;
using Parkin.Api.Features.Lots.List;
using Parkin.Api.Features.LotLayouts.Get;
using Parkin.Api.Features.Occupancy;
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
    IConfiguration config)
  {
    string? connectionString = config.GetConnectionString("AppDb");
    Guard.Against.Null(connectionString, "AppDb connection string is required. Make sure the application is running with Aspire.");

    services.AddScoped<AuditingInterceptor>();
    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      options.UseNpgsql(connectionString);
      options.AddInterceptors(
        provider.GetRequiredService<AuditingInterceptor>(),
        provider.GetRequiredService<EventDispatchInterceptor>());
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
           .AddScoped<ILotOccupancyQueryService, LotOccupancyQueryService>()
           .AddScoped<IListAccessEventsQueryService, ListAccessEventsQueryService>()
           .AddScoped<IAccessEventReplayQueryService, AccessEventReplayQueryService>()
           .AddScoped<IGateLotReader, GateLotReader>()
           .AddScoped<IListActiveSessionsByLotQueryService, ListActiveSessionsByLotQueryService>()
           .AddScoped<IListGrantsByLotQueryService, ListGrantsByLotQueryService>()
           .AddScoped<IActiveReservationChecker, ActiveReservationChecker>()
           .AddScoped<ILotRowLocker, LotRowLocker>()
           .AddScoped<IUnitOfWork, EfUnitOfWork>()
           .AddScoped<IStaffUserService, StaffUserService>()
           .AddScoped<IStaffAuthService, StaffAuthService>();

    return services;
  }
}
