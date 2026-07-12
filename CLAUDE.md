# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

- `backend/` — .NET 10 solution (`Parkin.slnx`). All current backend code lives here. Run backend commands from this directory.
- `frontend/` — **scaffolded shell** (Vite + React 19 + TypeScript, TanStack Router/Query, Tailwind v4 + shadcn/ui, axios, Zod). It has the app skeleton (login route, `_authenticated` layout, api client, UI kit) but **no parking-domain screens yet**. Run frontend commands (`pnpm dev`, etc.) from this directory.

### Project docs & specs

Read these before building parking features — they are the source of truth for the target domain:

- [`parking-management-system-prd.md`](parking-management-system-prd.md) — product requirements: user stories by epic (A–G), per-story acceptance criteria, and **P0/P1/P2 priorities** (P0 = v1 must-have).
- [`parking-management-system-architecture.md`](parking-management-system-architecture.md) — system architecture: domain model & aggregates, the two auth schemes (cookie for SPA, `X-Api-Key` for the gate), the critical Access-Events decision path, DB schema/invariants, and a suggested build sequence (§12).
- [`V1-MVP-TASKS.md`](V1-MVP-TASKS.md) — **the executable V1 MVP build plan**: an ordered, dependency-aware checklist of P0 tasks (T0.1 → T7.1), each with deliverables and acceptance criteria. Work it top-to-bottom; check tasks off as they land. Completed task should be marked as completed, so agent would understand what task to pick. Each task will be made one by one. One agent = one task.

**The target domain is a single-tenant Parking Management System** (parking lots/spaces, drivers/plates, access grants, reservations, parking sessions, an inbound Access Events API, and an `EntryDecisionService`).

### Important: template vs. target domain

The backend was generated from the **Ardalis Minimal Clean Architecture** template and still contains the template's **e-commerce demo domain** (`Product`, `Cart`, `Order`, `GuestUser`). None of the parking domain exists in code yet. When building features, follow the established patterns below but model the parking domain from the PRD/architecture docs — treat the Cart/Order/Product slices as worked examples to use, not as domain to preserve.

Confirmed stack decisions:
- **ID type: Guid for every parking aggregate.** Resolves doc discrepancy (architecture §3 tree showed `ParkingLotId` as Vogen `int`, §5 ER diagram uses `uuid`). Every parking-domain strongly-typed ID is `[ValueObject<Guid>]`, matching the ER diagram — no int-sentinel IDs for parking aggregates. The `int`-sentinel pattern stays only on legacy template aggregates (Product/Cart/Order) until retired per T0.5.
- **Database: PostgreSQL (Npgsql).** Wired throughout — `UseNpgsql` (`InfrastructureServiceExtensions`, `AppDbContextExtensions`, design-time `AppDbContextFactory`), `builder.AddPostgres("postgres")` in the AppHost, and Npgsql-flavored EF migrations (`uuid`, `timestamp with time zone`, `boolean`). New persistence work targets PostgreSQL.
- **Mediation: the source-generated `Mediator` library** (martinothamar), not MediatR. Both the docs and the code agree on this — see the Mediator section below.

## Build / run / test

All commands run from `backend/`:

```powershell
dotnet build                                    # TreatWarningsAsErrors=true — warnings fail the build
dotnet run --project src/Parkin.AspireHost      # full local stack (recommended): PostgreSQL + Papercut SMTP + API
dotnet run --project src/Parkin.Api             # API only, against a local PostgreSQL (ConnectionStrings:AppDb in appsettings.json)
```

- **Aspire** (`src/Parkin.AspireHost/AppHost.cs`) provisions a persistent PostgreSQL container (`AppDb`) and a Papercut SMTP container, then launches the API project. The API project hard-requires a connection string named **`AppDb`** (guarded in `InfrastructureServiceExtensions`).
- On startup the app **applies pending migrations and seeds data** (`MiddlewareConfig.UseAppMiddlewareAndSeedDatabase` → `SeedData.InitializeAsync`). Set `DatabaseOptions:RecreateOnStartup = true` (dev only) to drop & recreate the DB each launch.
- API docs: Scalar UI + OpenAPI at `/openapi/{documentName}.json` (development only).

### EF Core migrations

```powershell
dotnet ef migrations add <Name> --project src/Parkin.Api
dotnet ef database update    --project src/Parkin.Api
```

`AppDbContextFactory` provides the design-time context. Migrations live in `src/Parkin.Api/Infrastructure/Data/Migrations/`.

### Tests

Three test projects under `tests/`, all runnable via `dotnet test <project>.csproj` from `backend/` (run one project at a time — MSBuild rejects multiple `.csproj` args):
- `Parkin.UnitTests` — pure unit tests, xUnit + Shouldly + NSubstitute, no I/O.
- `Parkin.IntegrationTests` — DB-level invariants (partial unique indexes, idempotency), spins up `Testcontainers.PostgreSql` per fixture.
- `Parkin.FunctionalTests` — full HTTP pipeline via `WebApplicationFactory<Program>` (`Parkin.Api`'s `Program` class is `public partial` for this), backed by its own `Testcontainers.PostgreSql` container (`ParkinApiFactory`). RbacTests (role/endpoint 403 checks, added in T1.2) live here.

`.runsettings` at the repo root configures xUnit parallelization. Testcontainers-based tests require Docker running locally.

## Architecture & conventions

Single Web project organized by **vertical slices**, not layers. Folders: `Domain/`, `Infrastructure/`, `<Name>Features/`, `Configurations/`.

### Feature slices (REPR pattern via FastEndpoints)

Each feature folder (`ProductFeatures/`, `CartFeatures/`) groups one slice. A slice file colocates the **Endpoint + Request/Response + Validator + Mapper**, and (for write/complex ops) a **Command/Query + Handler**. Two patterns coexist:
- **Direct repository in the endpoint** for simple CRUD (e.g. `ProductFeatures/Create`).
- **Mediator command/query → handler** for orchestration and cross-aggregate logic (e.g. `CartFeatures/AddToCart`, `ProductFeatures/List`). The endpoint injects `IMediator`, sends the command, and maps the `Result<T>`.

Endpoints translate `Ardalis.Result` status into typed HTTP results (`Results<Ok<T>, NotFound, ValidationProblem, ProblemHttpResult>`). List endpoints use 1-based `page`/`per_page` pagination (`PagedResult<T>`, `Constants.DEFAULT_PAGE_SIZE`/`MAX_PAGE_SIZE`) and emit RFC-5988 `Link` headers.

### Mediator (not MediatR)

Uses **martinothamar `Mediator`** (source-generated, registered in `Configurations/MediatorConfig.cs` via `AddMediatorSourceGen`). Consequences:
- Handlers implement `ICommandHandler<,>` / `IQueryHandler<,>` and return **`ValueTask<T>`** (not `Task<T>`).
- Commands/queries implement `ICommand<Result<T>>` / `IQuery<Result<T>>`.
- Pipeline behaviors are registered in `MediatorConfig` (order matters); `LoggingBehavior` currently lives under namespace `Nimble.Modulith.Web` (a template leftover).

### Domain

- Aggregates live in `Domain/<Name>Aggregate/`, derive from `EntityBase<TEntity, TId>` + `IAggregateRoot`, use private EF constructors and `static Create(...)` factories, and expose behavior through methods (no public setters).
- **Strongly-typed IDs via Vogen** (`readonly partial struct`). Parking aggregates use `[ValueObject<Guid>]` (see ID type decision above). Legacy template aggregates still use `[ValueObject<int>]` with a `New => From(0)` sentinel meaning "not yet persisted"; EF assigns the real value on save. Every typed ID/value object must be registered in `Infrastructure/Data/Config/VogenEfCoreConverters.cs` (`[EfCoreConverter<...>]`) or EF can't map it.
- Domain events are collected on entities and dispatched **after** a successful `SaveChanges` by `EventDispatchInterceptor` → `MediatorDomainEventDispatcher`.
- Persistence: `Ardalis.Specification` repositories (`IRepository<T>`/`IReadRepository<T>` → `EfRepository<T>`). Query logic goes in `Specifications/` (e.g. `ProductByIdSpec`) or in dedicated query services (`IListProductsQueryService`) for read-optimized projections.
- EF config: one `IEntityTypeConfiguration` per entity in `Infrastructure/Data/Config/`, auto-applied via `ApplyConfigurationsFromAssembly`.

### Enforced architectural boundaries (NsDepCop)

`src/Parkin.Api/config.nsdepcop` is compiled with `NSDEPCOP01` as an **error** — violations fail the build. Current rules forbid:
- `Domain.*` → `Infrastructure.*`
- `*Features.*` → `Infrastructure.*` (feature slices depend on Domain abstractions, not concrete infra)
- `Domain.OrderAggregate.*` → `Domain.CartAggregate.*`

When adding parking aggregates/features, extend these rules to keep the dependency direction clean.

### Cross-cutting

- **Config / DI** is split into `Configurations/*Configs.cs` extension methods (`AddOptionConfigs`, `AddServiceConfigs`, `AddInfrastructureServices`, `AddMediatorSourceGen`) called from `Program.cs` — add new registrations there, not inline in `Program.cs`.
- **Logging**: Serilog (console) + OpenTelemetry via `Parkin.ServiceDefaults`.
- **Email**: `IEmailSender` → `MimeKitEmailSender` (points at the Papercut SMTP container in dev); `FakeEmailSender` available.
- Central package versions in `Directory.Packages.props`; shared MSBuild props (net10.0, nullable, `TreatWarningsAsErrors`) in `Directory.Build.props`.
- Comment TODO's or important things only, remove comments when they not necessary anymore, or when solved. Don't create summary comments.
