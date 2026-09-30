using FastEndpoints;
using FastEndpoints.Swagger;
using Parkin.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .AddLoggerConfigs();

builder.Services.AddOptionConfigs(builder.Configuration)
                .AddServiceConfigs(builder.Configuration)
                .AddAuthConfigs(builder.Configuration, builder.Environment);

builder.Services.AddFastEndpoints()
                .SwaggerDocument(o => o.ShortSchemaNames = true);

var app = builder.Build();

app.UseAppMiddleware();
await app.MigrateAndSeedDatabaseAsync();

app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
