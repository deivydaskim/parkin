using System.Text.Json.Serialization;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.Configurations;

public static class OptionConfigs
{
  public static IServiceCollection AddOptionConfigs(this IServiceCollection services, IConfiguration configuration)
  {
    services.ConfigureHttpJsonOptions(options =>
      options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName))
            .Configure<SeedAdminOptions>(configuration.GetSection(SeedAdminOptions.SectionName))
            .Configure<SeedOperatorOptions>(configuration.GetSection(SeedOperatorOptions.SectionName));

    return services;
  }
}
