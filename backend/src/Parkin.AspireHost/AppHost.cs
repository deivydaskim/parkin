var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
  .WithLifetime(ContainerLifetime.Persistent);

var appDb = postgres.AddDatabase("AppDb");

builder.AddProject<Projects.Parkin_Api>("api")
      .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
      .WithReference(appDb)
      .WaitFor(appDb)
      .WithUrl("https://localhost:5001");

builder.Build().Run();
