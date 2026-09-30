namespace Parkin.Api.Configurations;

public static class MediatorConfig
{
  public static IServiceCollection AddMediatorSourceGen(this IServiceCollection services)
  {
    services.AddMediator(options =>
    {
      options.ServiceLifetime = ServiceLifetime.Scoped;
      options.Assemblies = [typeof(MediatorConfig)];
      options.PipelineBehaviors = [typeof(RequestLoggingBehavior<,>)];
    });

    return services;
  }
}
