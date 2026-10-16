# Backend (Parkin.Api)

.NET 10 API organized by vertical slices, with Clean Architecture boundaries enforced by namespace. `Parkin.AspireHost` handles local orchestration and `Parkin.ServiceDefaults` provides OpenTelemetry, health checks and resilience. `README.md` has the project tree and tech stack.

## Commands (run from `backend/`; Docker must be running)

```powershell
dotnet build                                    # TreatWarningsAsErrors + NsDepCop errors fail the build
dotnet run --project src/Parkin.AspireHost      # recommended: persistent PostgreSQL container (AppDb) + API
dotnet run --project src/Parkin.Api             # API only; needs ConnectionStrings:AppDb pointing at local Postgres

dotnet test tests/Parkin.UnitTests/Parkin.UnitTests.csproj
dotnet test tests/Parkin.IntegrationTests/Parkin.IntegrationTests.csproj
dotnet test tests/Parkin.FunctionalTests/Parkin.FunctionalTests.csproj
dotnet test tests/Parkin.UnitTests/Parkin.UnitTests.csproj --filter "FullyQualifiedName~EntryDecisionTests"
```

- Run one test project per `dotnet test` call. MSBuild rejects multiple `.csproj` arguments.
- Migrations: use the `create-migration` skill (`dotnet ef migrations add <Name> --project src/Parkin.Api`). Output goes to `Infrastructure/Data/Migrations/`; `AppDbContextFactory` is the design-time factory.
- The SDK is pinned in `global.json` (10.0.100, rolls forward). Package versions are central in `Directory.Packages.props`: add a `<PackageVersion>` there and a version-less `<PackageReference>` in the `.csproj`.
- `Directory.Build.props` sets `TreatWarningsAsErrors`, nullable and implicit usings for every project. Only CS1591 (missing XML docs) is suppressed.

## Running and configuration

- `Parkin.Api` listens on `https://localhost:5001` and `http://localhost:5000`. In development, Scalar is at `/scalar` and OpenAPI at `/openapi/{documentName}.json`.
- Under Aspire, `AppDb` comes from the AppHost's persistent Postgres container. Run directly, `appsettings.json` points at a local database named `Parkin2`. `InfrastructureServiceExtensions` throws at startup if `ConnectionStrings:AppDb` is missing.
- On startup the API applies pending migrations and seeds roles, the `SeedAdmin`/`SeedOperator` users and, when `DatabaseOptions:SeedDemoData` is set, the demo lot. The base `appsettings.json` leaves the `SeedAdmin` password empty; dev credentials and `SeedDemoData: true` are in `appsettings.Development.json`. `DatabaseOptions:RecreateOnStartup = true` (dev only) drops and recreates the DB.
- CORS origins for the SPA come from `Cors:AllowedOrigins`.

## Enforced boundaries (NsDepCop, `src/Parkin.Api/config.nsdepcop`)

IMPORTANT: violations are build errors.

- `Features.*` must not reference `Infrastructure.*`.
- `Domain.*` must not reference `Infrastructure.*`, `Features.*` or `Web.*`.

So any read-optimized query or infra capability a slice needs is declared as an interface in the slice or in `Domain/Interfaces` (e.g. `IListDriversQueryService`, `IGateLotReader`, `IUnitOfWork`, `ILotRowLocker`). It is implemented in `Infrastructure/Data/Queries/` and registered in `Infrastructure/InfrastructureServiceExtensions.cs`.

## Writing a slice (`Features/<Area>/<Action>/`)

Use `Features/Lots/Create/` as the reference slice.

- `<Action>Endpoint.cs` is a FastEndpoints REPR endpoint with the request DTO and a `Validator<T>` in the same file. It stays thin: map the request to strongly typed IDs, send a command or query through `IMediator`, and convert the `Result` with `Web/ResultHttpExtensions` (`ToOkResult`, `ToCreatedResult`, `ToNoContentResult`). Return typed `Results<Ok|Created|NoContent, ValidationProblem, ProblemHttpResult>` and set `Summary(...)` (example request/response, one entry per status code), `Tags(...)` and `Description(...)` for OpenAPI.
- `<Action>Handler.cs` holds the `ICommand<Result<T>>`/`IQuery<Result<T>>` record and its handler. Orchestration and writes live here.
- Mediator is **martinothamar Mediator** (source generated), not MediatR. Handlers return `ValueTask<T>`. It is registered Scoped with one pipeline behavior, `RequestLoggingBehavior` (`Configurations/MediatorConfig.cs`).
- Status mapping (`ResultHttpExtensions.ToFailure`): Invalid → 400 ValidationProblem, NotFound → 404, Conflict → 409, Forbidden → 403, Unauthorized → 401, Unavailable → 503, CriticalError → 500, anything else → 400. For `Invalid`, errors are grouped by `ValidationError.Identifier`, so set it to the request property name (see `LotErrors`).
- Authorization goes in `Configure()` with `Roles(AccessPolicies.OperatorOrAbove)` or `Roles(AccessPolicies.AdminOnly)`. The acting staff user comes from `ICurrentUser` and is passed into commands as `ActorId`.
- Lists use 1-based `page`/`per_page`, `PagedResult<T>`, `Constants` page sizes, and RFC 5988 `Link` headers (`Web/PaginationLinkHeader`).
- Enums go over the wire as strings (`JsonStringEnumConverter` for FastEndpoints and minimal APIs) and are stored with `HasConversion<string>()`.
- DI and middleware registration goes in `Configurations/*Config(s).cs` extension methods, not inline in `Program.cs`. A slice that needs its own services exposes an `Add<Slice>Services()` extension (e.g. `IngestAccessEventServiceExtensions`) wired into `Configurations/ServiceConfigs.cs`.

## Domain

- Aggregates live in `Domain/<Name>Aggregate/`. They derive from `EntityBase<T, TId>` + `IAggregateRoot` and have a private EF constructor, a static `Create(...)` and behavior methods (no public setters). Expected failures return `Ardalis.Result`, not exceptions. Lifecycle transitions (archive/restore, deactivate/reactivate, revoke) are idempotent. `ParkingSpace` is an entity inside the `ParkingLot` aggregate.
- IDs are Vogen `[ValueObject<Guid>]`. IMPORTANT: every new ID/value object must be registered in `Infrastructure/Data/Config/VogenEfCoreConverters.cs`, or EF cannot map it. Each entity also needs an `IEntityTypeConfiguration` in `Infrastructure/Data/Config/` and a `DbSet` in `AppDbContext`. Use the `new-aggregate` skill.
- Query logic goes in `Specifications/` (Ardalis.Specification) used with `IRepository<T>`/`IReadRepository<T>`.
- Time: handlers inject `TimeProvider`; domain methods take `now` as a parameter.
- Pure domain services live in `Domain/Services/`. `EntryDecision.Decide(EntryDecisionContext)` is the entry allow/deny logic, and `OccupancyResult` computes occupancy.

## Persistence, transactions, auditing

- PostgreSQL via Npgsql. `AppDbContext` is also the ASP.NET Identity context (`ApplicationUser`).
- `EfRepository<T>` is the Ardalis `RepositoryBase`, so each `AddAsync`/`UpdateAsync`/`DeleteAsync` calls `SaveChanges` immediately. When a handler writes more than one aggregate, or must read and write under a lock, wrap the work in `IUnitOfWork.ExecuteInTransactionAsync` (EF execution strategy + single transaction). Serialize work per lot with `ILotRowLocker`/`IGateLotReader.LockAndLoadAsync` (`SELECT ... FOR UPDATE` on `"ParkingLots"`).
- Duplicates use two layers. The handler pre-check returns `Result.Invalid(...)` (400). Races are caught by partial unique indexes: `AppDbContext.SaveChangesAsync` turns Postgres unique violations into `UniqueConstraintViolationException` (with `ConstraintName`), which handlers catch and map to `Result.Conflict` (see `ReservationConflicts`, `LotErrors`). Uncaught, `Web/UniqueConstraintViolationExceptionHandler` returns a generic 409.
- Domain events deriving from `AuditableDomainEvent` become `audit_log` rows via `AuditingInterceptor` inside the same `SaveChanges` as the business change. It deduplicates by reference, so an event is audited once even across several saves in one transaction. When no domain event exists (e.g. a denied event for an unknown lot), add an `AuditLogEntry` through `IRepository<AuditLogEntry>`. Other domain events are dispatched after a successful save by `EventDispatchInterceptor`.

## EF configuration conventions

- No snake_case naming convention: tables and columns keep PascalCase, so raw SQL and index filters must quote them (`HasFilter("\"Status\" = 'Active'")`, `"ParkingLots"`). Name indexes explicitly (`ix_session_active_lot_plate`).
- Aggregate id: `.HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, TEntity, TId>>().HasVogenConversion()`. `HasVogenConversion()` only exists for non-nullable IDs. For a nullable ID column use `.HasConversion<VogenEfCoreConverters.XIdEfCoreValueConverter, VogenEfCoreConverters.XIdEfCoreValueComparer>()` (see `ParkingSessionConfiguration`).

## Auth

- **Staff**: ASP.NET Identity cookie (`parkin.auth`, 8h sliding) is the default scheme, with `SystemAdmin` and `Operator` roles and `/auth/login`, `/auth/me`, `/auth/logout`. It returns 401/403 instead of redirects. Development uses `SameSite=Lax` over http so the SPA on `:5173` works; production uses `Strict` + `Secure`. Disabling a user bumps the security stamp; `SecurityStampValidatorOptions.ValidationInterval` (30s) bounds how long the old cookie keeps working.
- **Gates**: `X-Api-Key` (`Infrastructure/ApiKeys/ApiKeyAuthenticationHandler`). Keys are SHA-256 hashed at rest, shown once on creation, and scoped to a parking lot. Gate endpoints opt in with `AuthSchemes(AuthenticationSchemes.ApiKey)` (the constant lives in `Authorization/` so slices don't reference Infrastructure). The lot comes from the `lot_id` claim via `ICurrentUser.RequiredLotId`, never from the request body. `RequiredId` is the API key id for gates and the staff user id for cookies.

## Access Events (critical path)

`POST /api/v1/access-events` → `IngestAccessEventHandler`. It requires an `Idempotency-Key` header: `AccessEventIdempotency` replays the stored decision for the same key and returns 409 when a key is reused for a different event. Inside a transaction it row-locks the lot (`IGateLotReader.LockAndLoadAsync`), normalizes the plate (`PlateNormalizer`), then:

- Enter: `EntryContextBuilder` → `EntryDecision.Decide`, which opens a `ParkingSession`.
- Exit: closes the most recent active session.

Every decision, allow or deny, is recorded as an `AccessEvent`. Staff can also record manual events (`RecordManual`).

## Tests

xUnit, Shouldly, NSubstitute. `.runsettings` runs assemblies and collections in parallel.

- **UnitTests**: no I/O. Domain tests under `Domain/`, handler and feature tests under `<Area>Features/`. Mock repositories, query services and `TimeProvider` (`Substitute.For<TimeProvider>()`) with NSubstitute. Domain methods get an explicit `now`.
- **IntegrationTests**: DB-level invariants (partial unique indexes, idempotency, concurrency, migrations) on Testcontainers PostgreSQL. Create `AppDbContext` on `PostgresFixture.ConnectionString` and call `Database.MigrateAsync()`. Migration tests move to a specific migration with `GetService<IMigrator>().MigrateAsync("<MigrationName>")`, seed data, then migrate to latest.
- **FunctionalTests**: full HTTP pipeline via `WebApplicationFactory<Program>`. Each class uses `IClassFixture<ParkinApiFactory>`, so it gets its own `postgres:16-alpine` container (Development environment, `SeedDemoData=false`). Methods in a class share that DB, so give created entities unique names (`$"... {Guid.NewGuid():N}"`). To authenticate, post the seeded admin credentials to `/auth/login` on a `CreateClient()` client (it keeps the cookie). For gate calls, create a lot-scoped key with `POST /api-keys` and send `X-Api-Key` and `Idempotency-Key`. Deserialize with `JsonSerializerDefaults.Web` plus `JsonStringEnumConverter`.
