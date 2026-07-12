---
name: create-migration
description: Add and apply an EF Core migration for Parkin.Api using the project's exact commands and design-time factory. Invoke as /create-migration <MigrationName>.
disable-model-invocation: true
---

# create-migration

Add (and optionally apply) an EF Core migration for the Parkin.Api backend.

**Migration name:** `$ARGUMENTS` (PascalCase, e.g. `AddParkingLot`, `AddAccessGrantIndex`). If empty, ask the user for one before running anything.

## Steps

1. **Run from `backend/`.** All EF commands target the `Parkin.Api` project, which owns the design-time `AppDbContextFactory` — no `--startup-project` needed.

2. **Add the migration:**
   ```powershell
   dotnet ef migrations add $ARGUMENTS --project src/Parkin.Api
   ```
   New files land in `src/Parkin.Api/Infrastructure/Data/Migrations/`. Open the generated `Up`/`Down` and confirm they match intent — expect Npgsql-flavored types (`uuid`, `timestamp with time zone`, `boolean`). If a new typed ID/value object appears unmapped, it's almost certainly a missing `[EfCoreConverter<...>]` in `Infrastructure/Data/Config/VogenEfCoreConverters.cs` (see the `new-aggregate` skill) — fix that and regenerate.

3. **Apply it** (needs a reachable PostgreSQL on the `AppDb` connection string — the Aspire `AppDb` container, or a local Postgres in `appsettings.json`):
   ```powershell
   dotnet ef database update --project src/Parkin.Api
   ```
   Note: on a normal app launch the startup pipeline already applies pending migrations and seeds data, so an explicit `database update` is mainly for verifying the migration in isolation.

## Notes & guardrails

- **Never hand-edit an applied migration.** To change it, roll back (`dotnet ef migrations remove --project src/Parkin.Api` while unapplied, or `database update <PreviousMigration>` first) and regenerate.
- Keep `Down` reversible.
- `dotnet build` must stay green afterward (`TreatWarningsAsErrors=true`).
- If `dotnet ef` is missing: `dotnet tool restore` (or `dotnet tool install --global dotnet-ef`).
