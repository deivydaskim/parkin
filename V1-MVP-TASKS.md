# V1 MVP — Executable Task List

Ordered, dependency-respecting build tasks for the **P0** V1 MVP. Each task is sized for a single agent run: it names its **dependencies**, **deliverables**, **acceptance criteria (AC)**, and the **patterns/files** to follow. Work top-to-bottom; don't start a task until its dependencies are checked off.

**Scope:** P0 stories only (Epics A–G). P1/P2 are in the backlog at the bottom.
**Axis:** full-stack vertical slices — each feature task delivers .NET API **and** React screens and logic.

**Global conventions (apply to every task):**
- Backend slice = `<X>Features/<Op>/<Op>Endpoint.cs` (request + `Endpoint<,>` + `Validator<T>` [+ `Mapper<,,>`]); orchestration adds `<Op>Handler.cs` (`ICommand/IQuery` + handler). Simple CRUD → `IRepository<T>` directly (copy `ProductFeatures/Create`); cross-aggregate → Mediator (copy `CartFeatures/AddToCart`).
- New aggregate = `Domain/<X>Aggregate/<X>.cs` (`EntityBase<T,TId>` + `IAggregateRoot`, private EF ctor, guarded public ctor, static `Create`) + Vogen `<X>Id.cs` + `Specifications/`.
- Every Vogen ID/VO → register in `Infrastructure/Data/Config/VogenEfCoreConverters.cs`, add `<X>Configuration.cs`, add `DbSet` to `AppDbContext`, then `dotnet ef migrations add <Name>`.
- Every new feature namespace → add `Disallowed <X>Features.* → Infrastructure.*` to `config.nsdepcop`.
- Reuse `PagedResult<T>` + `Constants.*_PAGE_SIZE` for lists; `Extensions/ResultExtensions.cs` for Result→HTTP.
- Frontend = one folder under `src/features/`, one route under `src/routes/_authenticated/`, one Zod schema reused as RHF resolver **and** response parser.
- `dotnet build` must stay clean (`TreatWarningsAsErrors=true`, NsDepCop `NSDEPCOP01` = error). Run from `backend/`.

---

## Phase 0 — Foundations (do first, once)

- [x] **T0.1 — Add Identity to the data layer.** Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore`; make `AppDbContext` also derive from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`; add `ApplicationUser : IdentityUser<Guid>` (DisplayName, Status). Seed roles (`SystemAdmin`, `Operator`) + first `SystemAdmin`.
  - **AC:** migration adds AspNet* tables; app starts and seeds one admin; existing build stays green.
- [x] **T0.2 — Decide the ID type and record it.** Resolve the doc discrepancy (arch §3 shows `ParkingLotId` as `int`, §5 ER diagram uses `uuid`). **Recommend Guid for all parking IDs.** Document the choice in `CLAUDE.md`. Apply consistently in every later aggregate.
  - **AC:** a one-line decision note exists; no int-sentinel IDs used for parking aggregates.
- [x] **T0.3 — Stand up test projects.** Ensure `Parkin.UnitTests` builds; add `Parkin.IntegrationTests` (Testcontainers.PostgreSql) and `Parkin.FunctionalTests` (WebApplicationFactory / Aspire.Hosting.Testing). Packages are already in `Directory.Packages.props`.
  - **AC:** `dotnet test` runs (even with 0 tests) across all three.
- [ ] **T0.5 — Retire template cruft (rolling).** Delete Product/Cart/Order/GuestUser slices + their NsDepCop rules **as each parking analogue replaces them** (we already have few features that we can use as examples, so dont need tempalte anymore). Also remove the leftover `TestId` in `CartId.cs` and the `Order.cs → Infrastructure.Data` using. Migration to clear everything.

---

## Phase 1 — Auth & Access Control (Epic A) — *lock RBAC early*

- [x] **T1.1 — A1 Staff login/logout.** *Deps: T0.1.* Identity cookie scheme (HttpOnly, Secure, SameSite=Strict). Endpoints `POST /auth/login`, `GET /auth/me`, `POST /auth/logout`. Account lockout after N failures; generic "invalid email or password" (no enumeration); salted hashes; HTTPS. FE: wire existing `routes/login.tsx` + auth Zustand store + `useCurrentUser`.
  - **AC:** valid creds set cookie + authenticated session; invalid → generic message; lockout triggers; logout clears cookie; `/auth/me` hydrates the SPA.
- [x] **T1.2 — A2 RBAC.** *Deps: T1.1.* FastEndpoints role policies (`SystemAdmin`, `Operator`); wrong role → **403** regardless of UI. FE: `RoleGate` (hide-only). *(RbacTests deferred.)* 
  - **AC:** every endpoint authorizes server-side; RbacTests assert 403 for wrong-role/endpoint pairs; UI hides disallowed actions.
- [x] **T1.3 — A4 Staff user management.** *Deps: T1.2.* Create staff w/ role, disable (bump security-stamp → sessions die immediately), change role — `SystemAdmin` only; all changes audit-logged. FE: users screens under `settings/`. *(Tests deferred.)*
  - **AC:** disabled user can't authenticate and existing session dies within the stamp-check interval; role change takes effect; actions audit-logged.
- [ ] **T1.4 — A5 API key management.** *Deps: T1.2.* Generate key (**shown once**, SHA-256 hashed at rest, store prefix for display), list, revoke — `SystemAdmin` only; ≥1 active key required for later ingestion; create/revoke audit-logged. Add `X-Api-Key` `AuthenticationHandler<ApiKeyAuthenticationOptions>` in `Infrastructure/ApiKeys`. FE: view-once generate dialog under `settings/`.
  - **AC:** key returned once then only as hash; revoked key rejected immediately; audit entries written.

*(A3 self-service password reset is P2 — admin resets directly.)*

---

## Phase 2 — Lots & Spaces (Epic B)

- [x] **T2.1 — B1/B2/B3 Lot CRUD + access mode + full behavior.** *Deps: T1.2.* `ParkingLot` aggregate. Endpoints `GET/POST /lots`, `GET/PATCH /lots/{id}`, `POST /lots/{id}/archive` (`Operator+`). Unique name (`ux_lot_name`), required IANA timezone, optional address; archive preserves history; capacity derived from active spaces; access mode OPEN/RESTRICTED; full behavior BLOCK/ALLOW_OVERFLOW. FE: `features/lots/` (LotForm, LotTable, AccessModeToggle, FullBehaviorSelect) + `routes/_authenticated/lots/`. *(Capacity hardcoded to 0 until T2.2 adds ParkingSpace; no Docker in this environment so `dotnet ef database update` / live Aspire+browser smoke test were not run — migration generated and build/typecheck verified only.)*
  - **AC:** duplicate lot name rejected; mode/full-behavior persist; archived lot hidden from operational lists but readable in history.
- [x] **T2.2 — B4 Space CRUD.** *Deps: T2.1.* `ParkingSpace` entity within the lot aggregate. `GET/POST /lots/{lotId}/spaces`, `PATCH /spaces/{id}`, `POST /spaces/{id}/deactivate` (`Operator+`). Label unique within lot (`ux_space_lot_label`); type GENERAL/RESERVED; deactivate preserves history; **block deactivating a RESERVED space that has an active reservation.** FE: `features/spaces/` (SpaceForm, SpaceTable). *No ordinal inputs (B5 is P2).*
  - **AC:** duplicate label within a lot rejected; deactivated space leaves capacity/live view but stays in history; reserved-with-active-reservation deactivation blocked.
  - *Reservation guard is a stub (`NoActiveReservationChecker` always returns false) until the Reservation aggregate lands in T4 — swap then. No Docker in this environment, so `dotnet ef database update` / Testcontainers / live Aspire+browser smoke test were not run; migration generated (`ux_space_lot_label` unique index + FK confirmed in the generated file) and `dotnet build` + unit tests (`ParkingLotSpaceTests`, `DeactivateSpaceHandlerTests`, 19/19 passing) + frontend `pnpm build` were verified.*

---

## Phase 3 — Drivers, Plates & Grants (Epic C)

- [ ] **T3.1 — C1 Drivers & plates.** *Deps: T1.2.* `Driver` aggregate + `Plate` entity. `GET/POST /drivers`, plates sub-resource, reassign-plate. Driver name + optional contact; normalized plate **unique across instance** (`ux_plate_normalized`); one plate → one driver at a time; reassignment audit-logged. Keep `plate` as a denormalized string (for later anonymization). FE: `features/drivers/` (DriverForm, PlateManager).
  - **AC:** duplicate normalized plate rejected; reassigning a plate moves it and writes an audit entry; a driver can hold multiple plates.
- [ ] **T3.2 — C2 Access grants.** *Deps: T2.1, T3.1.* `AccessGrant` aggregate. `POST /grants`, `POST /grants/{id}/revoke`, `GET /drivers/{id}/grants` (`Operator+`). Optional validFrom/validTo; expired/revoked denies restricted-lot entry; **no effect on OPEN lots**; audit-logged. Index `ix_grant_driver_lot_status`. FE: `features/grants/` (GrantForm, GrantList), DriverGrantsPanel.
  - **AC:** grant/revoke persist + audit; expired window is inert; grant on an OPEN lot changes nothing.

*(C3 CSV import is P1.)*

---

## Phase 4 — Reservations (Epic D)

- [ ] **T4.1 — D1 Create reservation.** *Deps: T2.2, T3.1.* `Reservation` aggregate (denormalized `lot_id`). `GET/POST /reservations` (`Operator+`). **At most one ACTIVE reservation per space** → `409` on conflict (`ux_reservation_active_space`); ≤1 active per driver per lot (`ux_reservation_active_driver_lot`); creating sets space type RESERVED + removes from general pool; reserved driver implicitly has lot access even if RESTRICTED. FE: `features/reservations/` (ReservationForm, ConflictAlert).
  - **AC:** second active reservation on a space returns 409; space flips to RESERVED; **integration test proves the partial unique index rejects the conflict under concurrency.**
- [ ] **T4.2 — D2 Reassign / cancel.** *Deps: T4.1.* `POST /reservations/{id}/reassign`, `POST /reservations/{id}/cancel`. Cancel ends active (optionally returns space to GENERAL); reassign ends prior + creates new **atomically** (never two active); audit-logged with actor + timestamp. FE: ReassignDialog.
  - **AC:** reassign never leaves two ACTIVE rows; cancel/reassign audit-logged; integration test covers the atomic swap.

---

## Phase 5 — ★ Access Events, Decision Engine & Occupancy (Epic E) — highest value & risk

- [ ] **T5.1 — E3 EntryDecisionService + OccupancyCalculator (pure, tested first).** *Deps: none beyond domain enums.* Pure domain services in `Domain/Services/`, no I/O. Implement exact precedence: (1) unknown+RESTRICTED→DENY `NOT_AUTHORIZED`; (2) RESTRICTED+no grant/reservation→DENY `NOT_AUTHORIZED`; (3) reserved holder→ALLOW `RESERVED` **bypassing full**; (4) general+full+BLOCK→DENY `LOT_FULL`; (5) else ALLOW `GENERAL`. `OccupancyCalculator`: `general free = max(0, capacity − active GENERAL sessions)`, overflow flag, reserved bypass. **Exhaustive unit tests — this is the core IP, cover well above the ~30% target.**
  - **AC:** every branch + edge case unit-tested; services take a materialized context and return decision/reason/pool/reserved-label with zero I/O.
- [ ] **T5.2 — E1/E2 Ingestion endpoint + session lifecycle.** *Deps: T5.1, T1.4, T3.2, T4.1.* `POST /api/v1/access-events` (API-key auth, required `Idempotency-Key`, always **200 + decision body**). `IngestAccessEventCommandHandler` in one transaction: idempotency check (`ux_access_event_idempotency`, replay returns original, no double-count) → `SELECT lot FOR UPDATE` → resolve plate→driver/grant/reservation/active-GENERAL-count → `Decide()` → ENTER+ALLOW opens session (pool, space_id only for reserved) / EXIT closes most-recent open session else records anomaly, **never negative** → persist immutable `AccessEvent` → audit. Index `ix_session_active_lot_pool`.
  - **AC:** ENTER→ALLOW opens a session; EXIT closes it; replayed idempotency key returns original with no double-count; EXIT with no open session records an anomaly and doesn't go negative. Integration test covers replay + ENTER/EXIT end-to-end.
- [ ] **T5.3 — E4 Live occupancy view.** *Deps: T5.2.* `GET /lots/{id}/occupancy` (`Operator`). General capacity/used/free (floored at 0), reserved count, over-capacity flag; updates per event. FE: `features/occupancy/` (OccupancyStats, OccupancyTable) — labeled "lot-level, gate-counted."
  - **AC:** numbers match derived occupancy; free never negative; over-capacity flag shows under ALLOW_OVERFLOW.
- [ ] **T5.4 — E5 Manual entry/exit.** *Deps: T5.2.* `POST /lots/{id}/manual-events` (`Operator`). Known or ad-hoc/unknown plate; same session logic; `source=MANUAL` + acting staff; audit-logged. FE: `features/gate/` ManualEntryForm.
  - **AC:** manual ENTER/EXIT flows through the same session path and is tagged + audited.
- [ ] **T5.5 — E6 Override a decision.** *Deps: T5.2.* `POST /access-events/{id}/override` (`Operator`). New event with `override_of` → original; records original+overridden+reason+actor; override-to-allow opens a session; audit-logged. FE: DecisionBanner + OverrideDialog.
  - **AC:** override is a new immutable event linked to the original; override-to-allow opens a session; audited.
- [ ] **T5.6 — E7 Reconcile / reset occupancy.** *Deps: T5.2.* `GET /lots/{id}/sessions?status=ACTIVE`, `POST /sessions/{id}/close`, `POST /lots/{id}/occupancy/reset` (`Operator`). Each action audit-logged with before/after counts + optional note. FE: `features/sessions/` (ActiveSessionsTable, CloseSessionDialog, ResetCountDialog).
  - **AC:** stale session close and count reset both adjust occupancy and write an audit entry with before/after.

---

## Phase 6 — Tabular Spaces View (Epic F)

- [ ] **T6.1 — F2 Tabular spaces view.** *Deps: T2.2, T4.1, T5.3.* Data table of every space (label, type, assigned driver if reserved, status, zone) + clearly-labeled aggregate occupancy indicator. Mostly FE composition over existing endpoints (add a read/query service if a joined projection is cleaner). Route `routes/_authenticated/lots/$lotId.spaces`.
  - **AC:** table lists all spaces with reserved-assignee names; aggregate occupancy shown and labeled lot-level. *(2D map F1 is P2.)*

---

## Phase 7 — Audit Log (Epic G)

- [ ] **T7.1 — G1 Audit query.** *Deps: T1.2 (writing has run since Phase 1 via the SaveChanges interceptor + explicit decision logging).* `GET /api/v1/audit?from&to&actor&entity` (`SystemAdmin`, read-only). Enforce append-only (revoke UPDATE/DELETE on the table for the app DB role). Indexes `ix_audit_occurred_at/actor/entity`. FE: `features/audit/` (AuditFilters, AuditTable).
  - **AC:** log filterable by date/actor/entity; not editable/deletable through the app; access decisions + admin mutations all appear.

*(G2 retention/export/erase is P1 — schema already supports it.)*

---

## Backlog (out of this MVP)

- **P1:** C3 CSV import · E8 stale-session auto-expiry job · E9 occupancy history/reports · G2 retention + export/anonymize · access/reservation emails · multi-lot dashboard.
- **P2:** A3 self-service password reset · B5 layout ordinals · F1 2D map · B6 drag-drop builder · driver portal & self-booking · SSO/OIDC/SAML · per-space sensing · waitlists/EV/valet/wayfinding.

## How to run / verify each task

- **Stack:** `dotnet run --project src/Parkin.AspireHost` (Postgres + Papercut + API; migrations auto-apply + seed). Frontend: `pnpm dev` in `frontend/`.
- **Per task, drive the real flow** (use `/verify`) — e.g. T1.2: call an `Operator`-only endpoint as `SystemAdmin`, confirm 403; T5.2: POST ENTER via `X-Api-Key` → ALLOW + open session, POST EXIT → close + decrement, replay idempotency key → no double-count.
- **Tests weighted by risk:** exhaustive units for `EntryDecisionService`/`OccupancyCalculator`; integration for partial-unique-index invariants + idempotency; `RbacTests` for role/endpoint pairs. Measure via `dotnet test --collect:"XPlat Code Coverage"` (~30% goalpost).
