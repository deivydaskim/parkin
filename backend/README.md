# Parkin Backend

Single-tenant Parking Management System API. It manages parking lots and spaces, drivers and plates, access grants, reservations and parking sessions. It also exposes an inbound Access Events API that decides, per gate event, whether a vehicle may enter.

The backend is a single Web project organized by **vertical slices** (features), following Clean Architecture principles without multiple projects.

## Getting Started

All commands run from the `backend/` directory. Docker must be running (PostgreSQL container, Testcontainers).

### Build and Run

```powershell
dotnet build

dotnet run --project src/Parkin.AspireHost

dotnet run --project src/Parkin.Api
```

`TreatWarningsAsErrors` is enabled, so warnings fail the build.

### Database Setup

The database is **PostgreSQL**.

#### Option 1: Run with Aspire (Recommended)

```powershell
dotnet run --project src/Parkin.AspireHost
```

The AppHost provisions a persistent PostgreSQL container with an `AppDb` database and launches the API. Pending migrations are applied and identity data is seeded on startup.

#### Option 2: Run Api project directly (local PostgreSQL)

Point `ConnectionStrings:AppDb` in `appsettings.json` at a local PostgreSQL instance, then:

```powershell
dotnet ef database update --project src/Parkin.Api
dotnet run --project src/Parkin.Api
```

The API requires a connection string named `AppDb`. In development only, set `DatabaseOptions:RecreateOnStartup = true` to drop and recreate the database on each launch.

API docs (Scalar UI and OpenAPI) are served in development at `/openapi/{documentName}.json`.

## Project Structure

```text
backend/
├── src/
│   ├── Parkin.Api/
│   │   ├── Domain/                # Aggregates, domain events, specifications, pure domain services
│   │   │   ├── AccessEventAggregate/
│   │   │   ├── AccessGrantAggregate/
│   │   │   ├── ApiKeyAggregate/
│   │   │   ├── AuditAggregate/
│   │   │   ├── DriverAggregate/
│   │   │   ├── ParkingLotAggregate/
│   │   │   ├── ParkingSessionAggregate/
│   │   │   ├── ReservationAggregate/
│   │   │   ├── Services/          # EntryDecision, OccupancyResult
│   │   │   └── StaffUsers/        # Staff user abstractions
│   │   ├── Features/              # Vertical slices: Features/<Area>/<Action>/
│   │   │   ├── AccessEvents/  ApiKeys/  Audit/  Auth/  Drivers/
│   │   │   ├── Grants/  LotLayouts/  Lots/  Occupancy/  Plates/
│   │   │   └── Reservations/  Sessions/  Spaces/  Users/
│   │   ├── Infrastructure/
│   │   │   ├── ApiKeys/           # X-Api-Key authentication handler
│   │   │   ├── Data/              # AppDbContext, Config/, Migrations/, Queries/, interceptors, SeedData
│   │   │   └── Identity/          # ASP.NET Identity staff user implementation
│   │   ├── Authorization/         # Policies, schemes, claim types
│   │   ├── Configurations/        # DI and middleware extension methods
│   │   ├── Web/                   # Result -> HTTP mapping, pagination headers, current user
│   │   └── Program.cs
│   ├── Parkin.AspireHost/         # Local orchestration (PostgreSQL + API)
│   └── Parkin.ServiceDefaults/    # OpenTelemetry, health checks, resilience
└── tests/
    ├── Parkin.UnitTests/
    ├── Parkin.IntegrationTests/
    └── Parkin.FunctionalTests/
```

### Key Design Decisions

- **Vertical Slices**: organized by feature (lots, drivers, grants, ...), not by layer.
- **REPR endpoints (FastEndpoints)**: each slice has an `Endpoint` with its request and validator, plus a Mediator command/query and handler. Endpoints stay thin; writes and orchestration live in handlers.
- **Mediator**: the source-generated [martinothamar Mediator](https://github.com/martinothamar/Mediator) library (not MediatR). Handlers return `ValueTask<T>`.
- **Domain-Driven Design**: aggregates derive from `EntityBase` and `IAggregateRoot`, use private constructors and `Create(...)` factories, and expose behavior through methods. Expected failures return `Result`, never exceptions.
- **Strongly-typed IDs**: Vogen `[ValueObject<Guid>]` for every aggregate. Each must be registered in `Infrastructure/Data/Config/VogenEfCoreConverters.cs`.
- **Auditing**: domain events deriving from `AuditableDomainEvent` become `audit_log` rows inside the same `SaveChanges` as the business change.
- **Two auth schemes**: cookie-based ASP.NET Identity for the operator console, and `X-Api-Key` (scoped to a parking lot) for gates.
- **Enforced boundaries**: NsDepCop (`config.nsdepcop`) fails the build if `Features.*` depends on `Infrastructure.*`, or `Domain.*` depends on `Infrastructure.*`, `Features.*` or `Web.*`.
- **Time**: handlers inject `TimeProvider`; domain methods take `now` as a parameter.

## Technology Stack

- **.NET 10**
- **FastEndpoints**: REPR pattern for API endpoints (with built-in validation)
- **Mediator** (source generated)
- **Entity Framework Core** with **PostgreSQL** (Npgsql provider) and migrations
- **Ardalis.Specification**, **Ardalis.Result**, **Ardalis.GuardClauses**
- **Vogen**: strongly-typed IDs
- **ASP.NET Core Identity**: staff users
- **Aspire**: local orchestration and observability
- **Serilog** and **OpenTelemetry**: logging and telemetry
- **Scalar**: API documentation UI
- **xUnit**, **Shouldly**, **NSubstitute**, **Testcontainers**

## Common Tasks

### Adding a New Feature (Vertical Slice)

1. **Create the aggregate**: add to `Domain/YourFeatureAggregate/` (typed ID, events, specifications).
2. **Register the ID converter**: add the Vogen ID to `Infrastructure/Data/Config/VogenEfCoreConverters.cs`.
3. **Add EF configuration**: create an `IEntityTypeConfiguration` in `Infrastructure/Data/Config/`.
4. **Create a migration**: `dotnet ef migrations add AddYourFeature --project src/Parkin.Api`
5. **Create the slice**: add endpoint, command/query and handler in `Features/YourFeature/<Action>/`.

### Database connection

Set the `AppDb` connection string in `appsettings.json` (Aspire injects it automatically when run via the AppHost):

```json
{
  "ConnectionStrings": {
    "AppDb": "Host=localhost;Port=5432;Database=Parkin;Username=postgres;Password=postgres"
  }
}
```

### Running Tests

Run one test project at a time (MSBuild rejects multiple `.csproj` arguments). Integration and functional tests start PostgreSQL via Testcontainers, so Docker must be running.

```powershell
dotnet test tests/Parkin.UnitTests/Parkin.UnitTests.csproj
dotnet test tests/Parkin.IntegrationTests/Parkin.IntegrationTests.csproj
dotnet test tests/Parkin.FunctionalTests/Parkin.FunctionalTests.csproj
```

- **UnitTests**: pure domain and handler tests, no I/O.
- **IntegrationTests**: database-level invariants (partial unique indexes, idempotency, concurrency, migrations).
- **FunctionalTests**: full HTTP pipeline through `WebApplicationFactory<Program>`.

## Resources

- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [Mediator](https://github.com/martinothamar/Mediator)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)
- [Ardalis Clean Architecture Template](https://github.com/ardalis/CleanArchitecture) (origin of the project structure)
