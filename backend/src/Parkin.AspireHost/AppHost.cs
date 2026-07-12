using System.Net.Sockets;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL container
var postgres = builder.AddPostgres("postgres")
  .WithLifetime(ContainerLifetime.Persistent);

// Add the database
var appDb = postgres.AddDatabase("AppDb");

// Papercut SMTP container for email testing
var papercut = builder.AddContainer("papercut", "jijiechen/papercut", "latest")
  .WithEndpoint("smtp", e =>
  {
    e.TargetPort = 25;   // container port
    e.Port = 25;         // host port
    e.Protocol = ProtocolType.Tcp;
    e.UriScheme = "smtp";
  })
  .WithEndpoint("ui", e =>
  {
    e.TargetPort = 37408;
    e.Port = 37408;
    e.UriScheme = "http";
  });

// register the API project and link the DB
builder.AddProject<Projects.Parkin_Api>("api")
      .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
      .WithReference(appDb)
      .WaitFor(papercut)
      .WaitFor(appDb)
      .WithUrl("https://localhost:5001"); // Match launchSettings

builder.Build().Run();
