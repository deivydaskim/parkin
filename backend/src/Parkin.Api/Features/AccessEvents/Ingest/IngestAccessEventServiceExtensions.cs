namespace Parkin.Api.Features.AccessEvents.Ingest;

public static class IngestAccessEventServiceExtensions
{
  public static IServiceCollection AddIngestAccessEventServices(this IServiceCollection services)
    => services
      .AddScoped<AccessEventIdempotency>()
      .AddScoped<EntryContextBuilder>();
}
