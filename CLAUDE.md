# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

- `backend/` — .NET 10 solution (`Parkin.slnx`). All current backend code lives here. Run backend commands from this directory.
- `frontend/` — the operator console SPA (Vite + React 19 + TypeScript, TanStack Router/Query, Tailwind v4 + shadcn/ui, axios, Zod). The `_authenticated` layout is a collapsible sidebar shell (`components/layout/`) with breadcrumbs driven by each route's `staticData.crumb`. Screens: dashboard, gate console, lots (+ spaces, 3D layout), drivers (+ plates, grants), and SystemAdmin-only staff / API keys / audit log. Shared building blocks (`PageHeader`, `EmptyState`, `ConfirmDialog`, `StatusBadge`, `SearchInput`, `DataTablePagination`, `EntityCombobox`, …) live in `src/components/`; design tokens (incl. `success`/`warning`/`info`) in `src/styles/globals.css`. List filters live in the URL via `validateSearch`. Run frontend commands (`pnpm dev`, etc.) from this directory.
- `gate/` — the gate simulator demo (Vite + React 19 + Tailwind v4, port 5174): a fake plate-reader gate that calls the real `POST /api/v1/access-events` through a Vite proxy that injects `X-Api-Key`. Run `pnpm gate:setup` (creates the API key, demo plates and `.env.local`; needs the API running) and `pnpm dev` from this directory.

### Project docs & specs

Read these before building parking features — they are the source of truth for the target domain:

- [`parking-management-system-prd.md`](parking-management-system-prd.md) — product requirements: user stories by epic (A–G), per-story acceptance criteria, and **P0/P1/P2 priorities** (P0 = v1 must-have).
- [`parking-management-system-architecture.md`](parking-management-system-architecture.md) — system architecture: domain model & aggregates, the two auth schemes (cookie for SPA, `X-Api-Key` for the gate), the critical Access-Events decision path, DB schema/invariants, and a suggested build sequence (§12).
- [`V1-MVP-TASKS.md`](V1-MVP-TASKS.md) — **the executable V1 MVP build plan**: an ordered, dependency-aware checklist of P0 tasks (T0.1 → T7.1), each with deliverables and acceptance criteria. Work it top-to-bottom; check tasks off as they land. Completed task should be marked as completed, so agent would understand what task to pick. Each task will be made one by one. One agent = one task.

**The target domain is a single-tenant Parking Management System** (parking lots/spaces, drivers/plates, access grants, reservations, parking sessions, an inbound Access Events API, and an `EntryDecisionService`).

### Origin

The backend started from the **Ardalis Minimal Clean Architecture** template; the template e-commerce demo domain has been removed and all code now models the parking domain from the PRD/architecture docs.

Confirmed stack decisions:
- **ID type: Guid for every parking aggregate.** Resolves doc discrepancy (architecture §3 tree showed `ParkingLotId` as Vogen `int`, §5 ER diagram uses `uuid`). Every parking-domain strongly-typed ID is `[ValueObject<Guid>]`, matching the ER diagram — no int-sentinel IDs for parking aggregates.
- **Database: PostgreSQL (Npgsql).** Wired throughout — `UseNpgsql` (`InfrastructureServiceExtensions`, `AppDbContextExtensions`, design-time `AppDbContextFactory`), `builder.AddPostgres("postgres")` in the AppHost, and Npgsql-flavored EF migrations (`uuid`, `timestamp with time zone`, `boolean`). New persistence work targets PostgreSQL.
- **Mediation: the source-generated `Mediator` library** (martinothamar), not MediatR. Both the docs and the code agree on this — see the Mediator section below.

## Build / run / test

All commands run from `backend/`:

```powershell
dotnet build                                    # TreatWarningsAsErrors=true — warnings fail the build
dotnet run --project src/Parkin.AspireHost      # full local stack (recommended): PostgreSQL + API
dotnet run --project src/Parkin.Api             # API only, against a local PostgreSQL (ConnectionStrings:AppDb in appsettings.json)
```

- **Aspire** (`src/Parkin.AspireHost/AppHost.cs`) provisions a persistent PostgreSQL container (`AppDb`) and launches the API project. The API project hard-requires a connection string named **`AppDb`** (guarded in `InfrastructureServiceExtensions`).
- On startup the app **applies pending migrations and seeds data** (`MiddlewareConfig.MigrateAndSeedDatabaseAsync` → `SeedData`). Set `DatabaseOptions:RecreateOnStartup = true` (dev only) to drop & recreate the DB each launch.
- API docs: Scalar UI + OpenAPI at `/openapi/{documentName}.json` (development only).

### EF Core migrations

```powershell
dotnet ef migrations add <Name> --project src/Parkin.Api
dotnet ef database update    --project src/Parkin.Api
```

`AppDbContextFactory` provides the design-time context (reads `ConnectionStrings__AppDb` from the environment, else a local default). Migrations live in `src/Parkin.Api/Infrastructure/Data/Migrations/`.

### Tests

Three test projects under `tests/`, all runnable via `dotnet test <project>.csproj` from `backend/` (run one project at a time — MSBuild rejects multiple `.csproj` args):
- `Parkin.UnitTests` — pure unit tests, xUnit + Shouldly + NSubstitute, no I/O.
- `Parkin.IntegrationTests` — DB-level invariants (partial unique indexes, idempotency), spins up `Testcontainers.PostgreSql` per fixture.
- `Parkin.FunctionalTests` — full HTTP pipeline via `WebApplicationFactory<Program>` (`Parkin.Api`'s `Program` class is `public partial` for this), backed by its own `Testcontainers.PostgreSql` container (`ParkinApiFactory`). RbacTests (role/endpoint 403 checks, added in T1.2) live here.

`.runsettings` at the repo root configures xUnit parallelization. Testcontainers-based tests require Docker running locally.

## Architecture & conventions

Single Web project organized by **vertical slices**, not layers. Folders: `Domain/`, `Infrastructure/`, `Features/<Area>/<Action>/` (namespace `Parkin.Api.Features.<Area>.<Action>`), `Web/`, `Authorization/`, `Configurations/`.

### Feature slices (REPR pattern via FastEndpoints)

Each action folder holds one slice: `<Verb><Entity>Endpoint` + its `Request` class + `Validator` in one file, and a Mediator `Command`/`Query` + `Handler` in another. Endpoints are thin: build the command (actor from `ICurrentUser`), `mediator.Send`, map the result. All writes, guardrails and orchestration live in handlers.

- One wire model per entity: `<Entity>Response` (plain `Guid` ids, static `From(...)` factory), returned directly by handlers and query services. No separate Dto/Record/Mapping layers.
- `Web/ResultHttpExtensions` is the single `Result` → HTTP map: `ToOkResult`, `ToCreatedResult`, `ToNoContentResult`, `ToHttpResult`, `ToFailure<T>` (Invalid→400 ValidationProblem, NotFound→404, Conflict→409, Forbidden→403, other errors→400 problem). Endpoint return types are `Results<Ok<T>, ValidationProblem, ProblemHttpResult>`-style unions.
- List endpoints use 1-based `page`/`per_page` pagination (`PagedResult<T>`, `Constants.DEFAULT_PAGE_SIZE`/`MAX_PAGE_SIZE`) and call `HttpContext.AppendPaginationLinks(page)` for RFC-5988 `Link` headers (other query parameters are preserved).
- Every query-service method takes a `CancellationToken`.

### Mediator (not MediatR)

Uses **martinothamar `Mediator`** (source-generated, registered in `Configurations/MediatorConfig.cs` via `AddMediatorSourceGen`). Consequences:
- Handlers implement `ICommandHandler<,>` / `IQueryHandler<,>` and return **`ValueTask<T>`** (not `Task<T>`).
- Commands/queries implement `ICommand<Result<T>>` / `IQuery<Result<T>>`.
- Pipeline behaviors are registered in `MediatorConfig` (order matters). `RequestLoggingBehavior` logs request name, result status and duration only — never payloads (plates are personal data).
- `MSG0005` (notification without handler) is suppressed: domain events may legitimately have no handlers.

### Domain

- Aggregates live in `Domain/<Name>Aggregate/`, derive from `EntityBase<TEntity, TId>` + `IAggregateRoot`, use private EF constructors and `static Create(...)` factories, and expose behavior through methods (no public setters). Expected failures (unknown child id, rule violations) return `Result`/`Result<T>` — aggregates never throw on user input. Lifecycle methods are idempotent (a no-op raises no event).
- **Strongly-typed IDs via Vogen** (`[ValueObject<Guid>]`, generated with `Guid.CreateVersion7()`). Every typed ID/value object must be registered in `Infrastructure/Data/Config/VogenEfCoreConverters.cs` (`[EfCoreConverter<...>]`) or EF can't map it.
- Time: never `DateTimeOffset.UtcNow` — handlers inject `TimeProvider` and pass `now` into domain methods.
- Pure domain rules are static functions (`EntryDecision.Decide`, `OccupancyResult.Calculate`), not DI services.
- **Auditing**: events deriving from `AuditAggregate/AuditableDomainEvent` (action, entity, actor, optional `Metadata`) are turned into `audit_log` rows by `Infrastructure/Data/AuditingInterceptor` inside the **same** `SaveChanges` as the business change. Never save audit rows separately and never write audit-only handlers; new audited behavior = new event type + `AuditActions` constant. `EventDispatchInterceptor` still publishes events after save for any future non-audit handlers.
- Staff users (ASP.NET Identity) sit behind `Domain/StaffUsers` abstractions (`IStaffUserService`, `IStaffAuthService`, `StaffRoles`, `UserStatus`) implemented in `Infrastructure/Identity`.
- Persistence: `Ardalis.Specification` repositories (`IRepository<T>`/`IReadRepository<T>` → `EfRepository<T>`). Use `GetByIdAsync` for simple loads; add a spec only for includes/filters; read-optimized projections go in query services (`Infrastructure/Data/Queries`). Multi-save units of work go through `IUnitOfWork`.
- Postgres unique violations are translated in `AppDbContext.SaveChangesAsync` into `Domain.Exceptions.UniqueConstraintViolationException` (with `ConstraintName`); unhandled ones become a 409 via `Web/UniqueConstraintViolationExceptionHandler`. Features never catch EF/Npgsql exceptions.
- EF config: one `IEntityTypeConfiguration` per entity in `Infrastructure/Data/Config/`, auto-applied via `ApplyConfigurationsFromAssembly`.

### Enforced architectural boundaries (NsDepCop)

`src/Parkin.Api/config.nsdepcop` is compiled with `NSDEPCOP01` as an **error** — violations fail the build. Rules forbid:
- `Features.*` → `Infrastructure.*` (slices depend on Domain abstractions; new slices are covered automatically)
- `Domain.*` → `Infrastructure.*`, `Features.*`, `Web.*`

### Cross-cutting

- **Config / DI** is split into `Configurations/*Configs.cs` extension methods (`AddOptionConfigs`, `AddServiceConfigs`, `AddInfrastructureServices`, `AddMediatorSourceGen`, `AddAuthConfigs`) called from `Program.cs` — add new registrations there, not inline in `Program.cs`.
- Startup (`MiddlewareConfig.MigrateAndSeedDatabaseAsync`) applies migrations and seeds identity **fail-fast**; only optional demo seeding may fail without stopping the app.
- **Logging**: Serilog (console) + OpenTelemetry via `Parkin.ServiceDefaults`.
- Central package versions in `Directory.Packages.props`; shared MSBuild props (net10.0, nullable, `TreatWarningsAsErrors`) in `Directory.Build.props`.
- Dont Leave Comments; Comment TODO's or important things only, remove comments when they not necessary anymore, or when solved. Don't create summary comments.
