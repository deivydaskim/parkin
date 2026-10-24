# V1 Refactor & Hardening — Executable Task List

Fixes, refactors and hardening found in the architecture review of the V1 codebase (commit `76fe8cd`, 2026-10-16). Same format as `V1-MVP-TASKS.md`: each task is sized for a single agent run and names its **dependencies**, **deliverables** and **acceptance criteria (AC)**. Work top-to-bottom within a phase and check a task off (`- [x]`) when it lands.

**Priority:** Phases R0–R1 are P0 (bugs, security, data integrity), R2–R3 are P1 (important refactors, architecture, test gaps), R4–R6 are P2 (improvements). P3 feature ideas are in the backlog at the bottom.

**Paths:** backend paths are relative to `backend/src/Parkin.Api/`, frontend paths to `frontend/src/`, gate paths to `gate/`. Line numbers are from the review commit and may drift; re-read the code before changing it.

**Global conventions (apply to every task):**
- Follow `CLAUDE.md`: vertical slices, `Ardalis.Result` for expected failures, Vogen IDs registered in `VogenEfCoreConverters.cs`, NsDepCop boundaries, `IUnitOfWork` for multi-aggregate writes, partial unique indexes for races.
- Every bug fix ships with a test that fails before the fix: unit test for domain logic, integration test (Testcontainers) for DB invariants, functional test for HTTP contracts.
- Schema changes go through `dotnet ef migrations add <Name>` (see the `create-migration` skill). A migration that adds a unique index must first clean up or fail loudly on existing duplicates.
- `dotnet build` stays clean (`TreatWarningsAsErrors`, NsDepCop). Frontend: `pnpm lint`, `pnpm test` and `pnpm build` stay green.

---

## Phase R0 — Gate & data integrity (backend, P0)

- [x] **R0.1 — One active session per plate per lot.** *Deps: none.* ENTER never checks whether the plate is already inside (`Features/AccessEvents/Ingest/EntryContextBuilder.cs:38-61`, `IngestAccessEventHandler.cs:69-92`), and `ix_session_active_lot_plate` is not unique (`Infrastructure/Data/Config/ParkingSessionConfiguration.cs:56-58`). An LPR double-read or a retry with a new `Idempotency-Key` opens a second session; `ExitAsync` closes only the most recent one, so the other stays open forever, inflates the General count and causes false `LotFull` denials on BLOCK lots. Two cars of one reserved driver can also both take the Reserved pool for a single space.
  - Under the existing lot row lock, detect an active session for the normalized plate. **Decision to record here:** recommended behaviour is ALLOW without opening a new session (barrier still opens on a double read, the event is recorded and points at the existing session); the alternative is DENY with a new `AlreadyInside` reason (anti-passback).
  - Make `ix_session_active_lot_plate` unique. Add a unique partial index on `SpaceId` for active Reserved sessions.
  - **AC:** a second ENTER for the same plate never creates a second active session; a concurrent double ENTER (integration test) leaves exactly one; occupancy counts are unchanged by the duplicate.

- [ ] **R0.2 — Archived drivers lose access.** *Deps: none.* `Driver.Archive` (`Domain/DriverAggregate/Driver.cs:43-49`) only flips status, and `EntryContextBuilder.MatchPlateAsync` (`EntryContextBuilder.cs:26-35`) checks `plate.Status` but never the driver's. An archived driver still enters RESTRICTED lots and keeps reserved-pool bypass. `CreateGrantHandler.cs:25`, `CreateReservationHandler.cs:35` and `ReassignReservationHandler.cs:30` also accept archived drivers because `DriverByIdSpec` does not filter on status.
  - Treat a plate whose driver is archived as unknown at the gate. Reject archived drivers in the grant, reservation and reassign handlers.
  - **AC:** an archived driver's plate is denied `NotAuthorized` on a RESTRICTED lot and gets no reserved bypass; creating a grant or reservation for an archived driver returns 400/409; restoring the driver restores access.

- [ ] **R0.3 — EXIT works on archived lots.** *Deps: none.* `IngestAccessEventHandler.cs:59-62` returns `LotArchived` before checking direction, and `Features/Lots/Archive/ArchiveLotHandler.cs:16` archives with active sessions open, so every parked car becomes a permanent phantom session that reappears if the lot is restored.
  - Let EXIT reach `ExitAsync` on archived lots (ENTER stays denied). **Decision to record here:** either block archiving while sessions are open (409 with the count) or close them as part of archiving with an audit entry.
  - **AC:** EXIT on an archived lot closes the session; archiving a lot with active sessions follows the recorded decision and is covered by a functional test.

- [ ] **R0.4 — Bound gate-supplied `OccurredAt`.** *Deps: none.* The validator only rejects `default` (`IngestAccessEventEndpoint.cs:103-105`). `OccurredAt` drives grant validity (`EntryContextBuilder.cs:46,63-67`) and session entry/exit time (`IngestAccessEventHandler.cs:85,105`), so a skewed clock or a stolen key can backdate an event onto an expired grant, and `ParkingSession.Close` (`Domain/ParkingSessionAggregate/ParkingSession.cs:57`) accepts exit before entry.
  - Reject events outside a configurable skew window around `ReceivedAt` (options class, validated on start), or evaluate grants at `ReceivedAt`. Clamp exit time to at least entry time in `ParkingSession.Close`.
  - **AC:** an event outside the window is rejected with 400 (or denied, per the 200 contract; record which); exit time is never earlier than entry time; unit tests for the clamp, functional test for the window.

- [ ] **R0.5 — Gate events are always `Source=Lpr`.** *Deps: none.* `IngestAccessEventEndpoint.cs:18,68,93-95` accepts `Lpr` or `Manual` from the body, so a machine key can create events that the console and audit show as manual staff entries.
  - Drop `Source` from the gate request and set `EventSource.Lpr` for API-key callers. Update `gate/` if it sends the field.
  - **AC:** a gate request cannot produce a `Manual` event; manual events still come only from `RecordManual` with an `ActorId`.

- [ ] **R0.6 — Login hardening and rate limiting.** *Deps: none.* `Features/Auth/Login/LoginHandler.cs:17-21` returns immediately for unknown or disabled emails (timing signal), and after 5 attempts only real accounts return 423 (`LoginEndpoint.cs:47`). There is no rate limiter, so anyone can keep `admin@…` locked out (5 tries / 15 min, `Infrastructure/InfrastructureServiceExtensions.cs:50-51`).
  - Run a dummy password hash on the miss path and return the same 401 for every failure, including lockout. Add `AddRateLimiter` in a `Configurations/*Config.cs`: per-IP on `/auth/login`, per-API-key on `/api/v1/access-events`.
  - **AC:** unknown, disabled, wrong-password and locked-out logins return identical responses; exceeding the login limit returns 429; gate traffic within normal rates is unaffected.

---

## Phase R1 — Frontend correctness & API contract (P0)

- [ ] **R1.1 — One blank-to-null rule across the API.** *Deps: none.* A blank lot address is sent as `undefined` (`features/lots/api.ts:30-37`), which `UpdateLotHandler.cs:33` reads as "unchanged" (`request.Address ?? lot.Address`), so the address can never be cleared while the UI reports success. Driver contact is sent as `''` (`features/drivers/api.ts:36-47`) and stored as-is, so `contact ?? '—'` renders blank cells (`DriverTable.tsx:66`, `routes/_authenticated/drivers/$driverId.tsx:84`, `DriverCombobox.tsx:42`). Spaces normalise blanks a third way (`features/spaces/api.ts:54-79`).
  - Pick one contract for optional text fields on PATCH (recommended: the backend trims and stores empty string as null; omitted means unchanged) and apply it to lots, drivers and spaces on both sides. Add a data migration converting existing `''` to null.
  - **AC:** clearing a lot address or driver contact persists null and the UI shows the fallback; request-body mappers have unit tests.

- [ ] **R1.2 — Grant validity dates are inclusive and timezone-aware.** *Deps: none (pairs with R2.1).* `features/grants/components/GrantForm.tsx:85-113` uses `type="date"` and `features/grants/api.ts:39-40` sends the bare `YYYY-MM-DD`. The backend parses it as midnight in the server's offset and checks `now <= ValidTo` (`Domain/AccessGrantAggregate/AccessGrant.cs:48`), so the chosen end day is excluded and picking today as "Valid to" fails validation.
  - Send `validFrom` as start of day and `validTo` as end of day in the lot's timezone (the audit page already does this, `routes/_authenticated/settings/audit.tsx:40-46`). Add a client-side `validTo >= validFrom` check.
  - **AC:** a grant valid "to today" admits entry until 23:59:59 lot time today and is denied tomorrow; same-day grants are accepted.

- [ ] **R1.3 — Session end clears cached data and keeps the user's place.** *Deps: none.* Logout removes only `qk.auth.me()` (`features/auth/queries.ts:47-50`) and navigates in-SPA (`components/layout/UserMenu.tsx:332-335`), so the next user on a shared terminal briefly sees the previous user's staff, API keys, audit and drivers. A 401 redirects to `/login` without a `redirect` param (`lib/api-client.ts:16-18`), and `/auth/me` stays cached for 5 minutes (`features/auth/queries.ts:17`), so pages render and then bounce.
  - Call `queryClient.clear()` on logout and on a 401. Redirect to `/login?redirect=<path+search>` and return there after sign-in.
  - **AC:** after logout and a different login, no previous-user data appears; after a session expiry, signing in returns to the original page.

- [ ] **R1.4 — Space sheet reflects mutations.** *Deps: none.* `features/spaces/components/SpacesPanel.tsx:50-52` falls back to the `selected` snapshot when the space drops out of the filtered list, so deactivating under the "Active" filter leaves the sheet showing Active with a Deactivate button.
  - Keep the sheet bound to fresh data (mutation result via `setQueryData`, or a space detail query), or close the sheet when the space leaves the list.
  - **AC:** after deactivate, reactivate or rename, the open sheet shows the new state or closes.

---

## Phase R2 — Backend architecture & tests (P1)

- [ ] **R2.1 — Expired grants don't block renewal.** *Deps: R1.2.* `Domain/AccessGrantAggregate/Specifications/ActiveGrantForDriverLotSpec.cs` and the unique partial index `ix_grant_driver_lot_status` (`Status='Active'`) ignore `ValidTo`, so a grant past its end date stays Active forever and `CreateGrantHandler.cs:35` rejects a renewal. Future-dated grants hit the same wall.
  - **Decision to record here:** add an `Expired` status (set lazily on read/create and by a sweep), or replace the index with a validity-window exclusion constraint (`tstzrange` + `btree_gist`). Use the same window in the pre-check.
  - **AC:** a renewal can be created once the old grant's window has ended; overlapping active windows for the same driver and lot are still rejected at the DB level.

- [ ] **R2.2 — Robust plate normalization.** *Deps: none.* `Domain/DriverAggregate/PlateNormalizer.cs:6` only strips spaces, so `AB-123`, `AB.123` and Unicode look-alikes normalize differently: LPR reads fail to match registered plates and `ux_plate_normalized` admits near-duplicates.
  - Apply NFKC, upper-case invariant, keep letters and digits only. Add a data migration that re-normalizes plates, sessions and access events, and fails with a report if it would create collisions.
  - **AC:** `AB-123`, `ab 123` and `AB.123` all match the same plate at the gate; unit tests cover separators and Unicode forms.

- [ ] **R2.3 — Consistent idempotent lifecycle transitions.** *Deps: none.* Contradicts CLAUDE.md ("lifecycle transitions are idempotent"):
  - Space deactivate/reactivate (`Domain/ParkingLotAggregate/ParkingLot.cs:154-175`) add an event and audit row on every repeat.
  - Grant revoke returns 400 on repeat (`Domain/AccessGrantAggregate/AccessGrant.cs:50-55`, asserted by `tests/Parkin.FunctionalTests/DriverFeatures/DriverLifecycleTests.cs:144`).
  - API key revoke returns 409 (`Domain/ApiKeyAggregate/ApiKey.cs:50`); reservation cancel returns 400 (`Domain/ReservationAggregate/Reservation.cs:64-67`).
  - Make every transition a successful no-op on repeat with no event raised, and update the tests.
  - **AC:** repeating any archive/restore, deactivate/reactivate, revoke or cancel returns success and writes no new audit row.

- [ ] **R2.4 — Transaction and event-dispatch safety.** *Deps: none.* `Infrastructure/Data/EventDispatcherInterceptor.cs:7-17` dispatches on every `SavedChangesAsync`, and `EfRepository` saves on each Add/Update, so inside `ExecuteInTransactionAsync` events go out before commit. If `EnableRetryOnFailure` is on (Aspire's `AddNpgsqlDbContext` enables it by default), `Infrastructure/Data/EfUnitOfWork.cs:10-16` would retry with a stale change tracker, the reference-based dedupe in `AuditingInterceptor.cs:9,30` would skip audit rows, and the raw transaction in `Infrastructure/Identity/StaffUserService.cs:104-110` would throw.
  - Dispatch non-audit domain events only after the outermost transaction commits (or remove the dispatcher until a handler exists). Make `EfUnitOfWork` retry-safe (execution strategy, tracker cleared per attempt) and route `StaffUserService` through `IUnitOfWork`.
  - **AC:** an integration test shows no event is dispatched for a rolled-back transaction; a simulated transient failure retries and writes each audit row exactly once.

- [ ] **R2.5 — Authorization coverage test.** *Deps: none.* RbacTests were deferred in T1.2; only `AuditQueryTests` and `StaffUserTests` assert 403.
  - Add a functional test that walks `EndpointDataSource` and fails if an endpoint has no `Roles`, `AllowAnonymous` or the ApiKey scheme. Add a table-driven test asserting Operator → 403 for every AdminOnly endpoint, and API key → 401/403 on staff endpoints.
  - **AC:** removing `Roles(...)` from any endpoint fails the test suite.

- [ ] **R2.6 — Production-config functional tests.** *Deps: none.* Functional tests run only in Development (`tests/Parkin.FunctionalTests/ParkinApiFactory.cs:27`), so the production cookie (Strict/Secure), HSTS and health-endpoint configuration are never exercised.
  - Add a Production-environment factory variant and tests for cookie flags, HSTS and health endpoints.
  - **AC:** a regression in production auth cookie settings fails a test.

---

## Phase R3 — Frontend consistency & tests (P1)

- [ ] **R3.1 — Shared error state.** *Deps: none.* A 500, a network failure or a `schema.parse` failure renders as empty data ("No drivers yet", "No staff accounts", "Lot not found") in `routes/_authenticated/drivers/index.tsx:113-141`, `lots/index.tsx:105-136`, `settings/users.tsx:41-45`, `settings/api-keys.tsx:30-38`, `settings/audit.tsx:97-104`, `features/sessions/components/ActiveSessionsPanel.tsx:30-40`, `drivers/$driverId.tsx:56` and `lots/$lotId.tsx:92`.
  - Add a shared `ErrorState` in `components/` (message from `parseApiError`, Retry calling `refetch`) and use it wherever a query can fail.
  - **AC:** a failing list or detail query shows an error with retry, never an empty state; 404 still shows "not found".

- [ ] **R3.2 — One server-error-to-field helper.** *Deps: none.* The mapping is copy-pasted in `features/drivers/components/DriverForm.tsx:43-57`, `features/lots/components/LotForm.tsx:59-73`, `features/spaces/components/SpaceForm.tsx:78-91` and `features/users/components/CreateUserDialog.tsx:55-68`. LotForm drops nested keys like `Layout.WidthMeters`; GrantForm and CreateApiKeyDialog don't map server errors at all.
  - Extract `applyServerErrors(form, error, fieldMap?)` into `lib/` (handles dotted paths and case), use it in all six forms, and unit-test it.
  - **AC:** a backend validation error on any form field, nested ones included, shows under that field.

- [ ] **R3.3 — Don't validate hidden fields.** *Deps: none.* `features/lots/schemas.ts:73-74` validates `layout` when `hasLayout` is false, and `features/spaces/schemas.ts:84-85` validates `placement` when `isPlaced` is false. The inputs are unmounted (`LotForm.tsx:177`, `SpaceForm.tsx:168`), so a stale invalid value silently blocks submit.
  - Use a discriminated union or `superRefine` keyed on the toggle.
  - **AC:** toggling a section off with an invalid value inside still allows submit; schema unit tests cover both states.

- [ ] **R3.4 — Confirm role changes.** *Deps: none.* `features/users/components/UserTable.tsx:110-125` promotes a user to SystemAdmin with a single click, while disable/enable use `ConfirmDialog`.
  - Route role changes through `ConfirmDialog`.
  - **AC:** no role change happens without an explicit confirmation.

- [ ] **R3.5 — Complete cache invalidation.** *Deps: none.* `features/spaces/queries.ts:34-43` (`invalidateLotSpaceViews`) skips `qk.lots.list()` and `qk.occupancy.all()`, though LotCard shows capacity and the dashboard sums it. Lot archive/restore also skip `qk.occupancy.all()`.
  - Add the missing keys.
  - **AC:** after a space or lot change, the lot list and dashboard reflect it without waiting for polling or `staleTime`.

- [ ] **R3.6 — Frontend test infrastructure and critical-path tests.** *Deps: R1.1, R3.2 (tests their output).* Only `features/lot-view/geometry.test.ts` and `auto-arrange.test.ts` exist, with no DOM environment.
  - Add jsdom, Testing Library and MSW. Cover: `lib/api-error.ts` (`parseApiError`), request-body mappers (`toLotBody`, `toUpdateSpaceInput`, driver/grant mappers), `features/gate/decision.ts` (`describeOutcome`), idempotency-key reuse on retry (`features/gate/queries.ts:41-47`), Zod schemas against recorded API fixtures, and the auth guard plus 401 redirect.
  - **AC:** `pnpm test` runs DOM tests; each listed module has at least one test.

---

## Phase R4 — Backend improvements (P2)

- [ ] **R4.1 — Foreign keys and the access-events index.** *Deps: R0.1 (same tables).* `AccessEvents`, `ParkingSessions`, `Reservations` and `AccessGrants` have no FKs to lots, drivers or spaces (`Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs:704-725` has FKs only for ApiKey, Plate and ParkingSpace). `Infrastructure/Data/Queries/ListAccessEventsQueryService.cs:13-18,78` sorts and counts by lot on the fastest-growing table with no supporting index. Also correct the T5.2 note in `V1-MVP-TASKS.md` that calls `access_events.lot_id` an FK.
  - Add restricting FKs and a `(LotId, OccurredAt DESC)` index on `AccessEvents`.
  - **AC:** migration applies cleanly on seeded data; orphan inserts fail at the DB.

- [ ] **R4.2 — Logging cleanup.** *Deps: none.* `Configurations/LoggerConfigs.cs:9-14` adds `.WriteTo.Console()` on top of the Console sink in `appsettings.json`, and `AddSerilog` leaves the default console provider, so lines are written two or three times. `WithMachineName`/`WithThreadId` enrichers need unreferenced packages, and `Serilog.Sinks.OpenTelemetry` is referenced but unused.
  - Configure sinks in one place, clear default providers, add or remove enricher packages, and wire or drop the OpenTelemetry sink.
  - **AC:** each log event is written once per sink; no unused logging packages.

- [ ] **R4.3 — Health checks and gate metrics.** *Deps: none.* The only health check is "self" (`src/Parkin.ServiceDefaults/Extensions.cs:100-107`), there is no Postgres readiness check, and the endpoints are mapped only in Development.
  - Add a DbContext/Npgsql readiness check and map liveness/readiness in all environments (anonymous, minimal output). Add metrics: gate decisions by direction and reason, ingest latency, lot-lock wait time.
  - **AC:** readiness fails when Postgres is down; the metrics appear in the Aspire dashboard.

- [ ] **R4.4 — Startup and configuration hygiene.** *Deps: none.*
  - Migrations and seeding run on every start in every environment (`Configurations/MiddlewareConfig.cs:41-75`), which races with multiple replicas: make it opt-in by config outside Development.
  - No options use `ValidateOnStart`; `Cors:AllowedOrigins` silently falls back to localhost: validate options at startup and require CORS origins outside Development.
  - `SeedData.cs:105-109` returns early when the admin exists, so the operator is never seeded on an existing DB: seed each user independently.
  - No security headers: add `X-Content-Type-Options`, frame options / `frame-ancestors`, and a CSP for API responses.
  - **AC:** a missing required option fails startup with a clear message; the operator is seeded on an existing DB; headers are present (functional test).

- [ ] **R4.5 — Remove dead and risky code.** *Deps: none.* `Infrastructure/Data/AppDbContextExtensions.cs` is unused and registers a DbContext without the auditing interceptor, so anyone who calls it bypasses the audit log: delete it. `DenyUnknownLotAsync` (`IngestAccessEventHandler.cs:121-144`) appears unreachable for gates because `ApiKey.LotId` is a restricting FK and lots are never deleted: confirm and remove, or keep it with a test proving the path.
  - **AC:** build green; no DbContext registration path skips the auditing interceptor.

- [ ] **R4.6 — Optimistic concurrency.** *Deps: none.* No concurrency token on `ParkingLot` or `Driver`, so concurrent edits silently overwrite each other. The last-active-admin check (`Domain/StaffUsers/UserGuardrails.cs`) is check-then-act, so two admins disabling each other at once can both succeed.
  - Map Postgres `xmin` as a concurrency token on `ParkingLot` and `Driver` and map `DbUpdateConcurrencyException` to 409. Make the last-admin guard atomic (serializable transaction or advisory lock).
  - **AC:** integration tests show a stale update returns 409 and two concurrent admin disables leave at least one active admin.

- [ ] **R4.7 — Audit log vs planned anonymization (G2).** *Deps: none (decision task).* Every gate event writes an audit row whose metadata includes the plate (`Domain/AccessEventAggregate/Events/AccessEventRecordedEvent.cs:14-25`). The table is append-only via `REVOKE` (`20260805194503_AuditLogAppendOnly.cs`), which only holds with a separate non-owner runtime DB role.
  - Decide and document: keep plates out of audit metadata (reference the access event ID), or give G2 a privileged migration-role path. Document the required runtime vs migration DB roles.
  - **AC:** the decision is recorded in `CLAUDE.md`; G2 has a viable anonymization path.

- [ ] **R4.8 — Small handler fixes.** *Deps: R0.2.*
  - `ReassignReservationHandler.cs:27-39` mutates the tracked aggregate before validating the target driver and never re-checks that the space is still active: validate first.
  - `CreateReservationHandler.cs` doesn't check the lot is Active: add it.
  - `ParkingLot.UpdateSpace` (`ParkingLot.cs:150`) emits `SpaceUpdatedEvent` when nothing changed: skip no-op updates.
  - `EntryContextBuilder.MatchPlateAsync` loads a tracked Driver with all plates while holding the lot lock: use a no-tracking projection.
  - **AC:** unit/functional tests for each behaviour change; no audit row for a no-op space update.

---

## Phase R5 — Frontend improvements (P2)

- [ ] **R5.1 — Show times in the lot's timezone.** *Deps: R1.2.* `lib/format.ts:11-34` uses browser-local `toLocale*`, although each lot has a `timezone`; `@date-fns/tz` is installed but unused. Format lot-scoped timestamps (sessions, access events, grants) in the lot timezone and label it.
  - **AC:** an operator in another timezone sees lot-local times with a timezone label.

- [ ] **R5.2 — Don't lose the one-time API key.** *Deps: none.* `features/api-keys/components/CreateApiKeyDialog.tsx:75-79` blocks only outside clicks; Escape and the dialog's close X (`components/ui/dialog.tsx:51`) still discard the key. `handleCopy` (lines 60-65) doesn't guard against `navigator.clipboard` being undefined on non-secure origins.
  - Require an explicit "I've copied the key" close while the key is shown, and fall back to select-to-copy when the clipboard API is missing.
  - **AC:** the key can't be dismissed by Escape or X before confirmation; copy works or degrades gracefully over plain HTTP.

- [ ] **R5.3 — Searchable timezone picker.** *Deps: none.* `features/lots/components/TimezoneSelect.tsx:9,29-35` is a Radix Select with ~400 options. Reuse `EntityCombobox`/`Command`.
  - **AC:** typing filters timezones; keyboard selection works.

- [ ] **R5.4 — Friendly env-config error.** *Deps: none.* `config/env.ts:7` throws at import and yields a blank page. Mirror the gate's error screen (`gate/src/config/env.ts:29-43`).
  - **AC:** an invalid `VITE_API_BASE_URL` shows a readable error page.

- [ ] **R5.5 — Lint and build tooling.** *Deps: none.*
  - Make `eslint.config.js:16` type-aware and enable `no-floating-promises` / `no-misused-promises`; add `@tanstack/eslint-plugin-query`.
  - Run `tsc -b` before `vite build` in `package.json:8` so type errors surface first.
  - Enable `noUncheckedIndexedAccess` and fix the fallout.
  - Split the three.js vendor chunk or raise the warning limit for that route only.
  - **AC:** `pnpm lint` and `pnpm build` clean with the stricter config.

- [ ] **R5.6 — Pagination in the URL for detail panels.** *Deps: none.* Page state is component state in `DriverGrantsPanel.tsx:28`, `LotGrantsPanel.tsx:29` and `ActiveSessionsPanel.tsx:26`, against the URL-filters convention; revoking the last item on a page leaves an empty page.
  - Move page state into the route's `validateSearch` and step back a page when the current one empties.
  - **AC:** panel pages survive reload and back/forward; no empty page after removing the last row.

- [ ] **R5.7 — Role-gate cleanup and prop conventions.** *Deps: none.*
  - 21 `RoleGate` / `useHasRole('Operator','SystemAdmin')` checks always pass because every user has one of those roles (`features/auth/schemas.ts:3`), making fallbacks unreachable (e.g. `features/spaces/components/SpaceSheet.tsx:82-88`, `canOperate` in `routes/_authenticated/lots/$lotId.tsx:55,160-161`). Remove them and gate only admin-only UI.
  - Replace inline prop types with `type Props` in `SpaceSheet.tsx:47`, `features/occupancy/components/OccupancyStats.tsx:44,63,71,122,143`, `RecentActivity.tsx:71`. Replace non-augmentation `interface`s in `features/auth/store.ts:4`, `lib/api-error.ts:15`, `routes/__root.tsx:5`.
  - **AC:** no always-true role checks remain; conventions match `CLAUDE.md`.

- [ ] **R5.8 — Profile 3D bay labels.** *Deps: none.* `features/lot-view/components/SpaceBays.tsx:167-190` renders up to 150 (`LABELS_AUTO_LIMIT`) drei `<Html>` overlays, all re-projected every frame while orbiting. Profile on a 100-bay lot; if frame time suffers, switch to troika text or sprites.
  - **AC:** orbiting a 100-bay lot stays smooth (record the before/after numbers here).

---

## Phase R6 — Gate simulator (P2)

- [ ] **R6.1 — Narrow the key-injecting proxy.** *Deps: R0.5.* `gate/vite.config.ts:11-17` adds `X-Api-Key` to every `/api/v1/*` path and method on both dev and preview, so anyone who can reach port 5174 (e.g. with `--host`) gets an authenticated relay. Proxy only `POST /api/v1/access-events` and bind to localhost.
  - **AC:** other paths and methods through the proxy carry no key; the simulator still works.

- [ ] **R6.2 — `gate:setup` doesn't pile up keys.** *Deps: none.* `gate/scripts/setup.ts:106-131` mints a new key whenever the stored one isn't reusable but never revokes the old one. Revoke prior keys created by the script (by name or prefix) before minting.
  - **AC:** running setup repeatedly leaves exactly one active simulator key per lot.

---

## Backlog — feature ideas (P3, not scheduled)

- **API key lifecycle:** `LastUsedAt`, optional expiry, overlapping rotation (today revoke-and-reissue starts a fresh idempotency scope).
- **Grant schedules:** time-of-day and weekday windows using `ParkingLot.Timezone`, which is stored and validated but never read.
- **LPR confidence:** a confidence field plus OCR look-alike matching (O/0, I/1, B/8), with a "low confidence, needs review" deny reason.
- **Live updates:** an after-commit outbox (builds on R2.4) feeding SSE or webhooks for gate decisions and occupancy, replacing the 10–15 s polling (`features/occupancy/queries.ts:5-6`, `features/gate/queries.ts:19`).
- **Visitor passes:** single-use grants consumed on entry, with a per-lot anti-passback setting (builds on R0.1).
- **Global search (Ctrl+K):** jump to a driver, plate, active session or lot; `cmdk` and `components/ui/command.tsx` already exist.
- **Driver history tab:** sessions and access events per driver on `drivers/$driverId.tsx` (needs a backend filter by driver).
- **Dashboard cards:** "recent denials" (pairs with T5.5 override) and "grants expiring soon".
