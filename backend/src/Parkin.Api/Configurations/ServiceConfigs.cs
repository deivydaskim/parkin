using Parkin.Api.Features.AccessEvents.Ingest;
using Parkin.Api.Infrastructure;
using Parkin.Api.Web;

namespace Parkin.Api.Configurations;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, ConfigurationManager configuration)
  {
    services.AddSingleton(TimeProvider.System)
            .AddHttpContextAccessor()
            .AddScoped<ICurrentUser, HttpContextCurrentUser>()
            .AddProblemDetails()
            .AddExceptionHandler<UniqueConstraintViolationExceptionHandler>()
            .AddInfrastructureServices(configuration)
            .AddIngestAccessEventServices()
            .AddMediatorSourceGen();

    return services;
  }
}
