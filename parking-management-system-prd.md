# Product Requirements Document — Parking Management System

| | |
|---|---|
| **Status** | v0.1 |
| **Date** | 2026-07-12 |
| **Stack (fixed)** | React + TypeScript · .NET 10 · PostgreSQL |
| **Deployment model** | Single-tenant — one deployed instance + one database per client company |

> **Terminology note:** Throughout this document, a "general" (a.k.a. unreserved) space means *unassigned, first-come-first-served* — not "no cost." This product handles **no payments**, so "free" only ever means "not reserved for a specific driver."

---

## 1. Executive Summary

This is a **B2B, single-tenant parking management platform** for operators of **private commercial lots and corporate campuses**. Each client company runs its own dedicated instance, where administrators define one or more parking lots, configure each lot as **open** (any driver may enter) or **restricted** (only granted drivers), and designate individual spaces as **general** or **reserved-for-a-specific-driver**. Occupancy is tracked **at the lot level by counting vehicle entries and exits**, fed in through an **inbound Access Events API** that an external license-plate-recognition (LPR) / barrier-gate system calls — the platform does not include or control that hardware. Drivers do not self-register or self-book in v1; access grants and reserved spaces are created and managed entirely by company staff. The core problem it solves: corporate and private-lot operators today manage access lists, reserved bays, and "is the lot full?" with spreadsheets, paper, and tribal knowledge, and have no single source of truth tying a license plate to a driver, a reservation, an access right, and a live occupancy count.

---

## 2. Problem Statement

Operators of private and corporate parking today juggle several disconnected problems:

- **Access is managed manually and goes stale.** Who is allowed into the lot lives in spreadsheets, emails, or a gate guard's memory. When an employee leaves or a visitor's day ends, the "list" is rarely updated, so access rights drift out of sync with reality. *Experienced by:* facilities/parking managers and security staff. *Cost of not solving:* unauthorized parking, disputes, and a standing security gap.
- **Reserved bays are unenforceable and undocumented.** Executives, tenants, or specific employees are promised a dedicated space, but there is no system of record mapping *space → driver*, no way to reassign cleanly, and no way to see at a glance which bays are spoken for. *Experienced by:* managers and the drivers who hold (and lose) their reserved bays. *Cost:* friction, escalations, and double-assignment.
- **"How full is the lot?" has no reliable answer.** Without a system that reconciles entries against exits, occupancy is a guess. *Experienced by:* security/reception (fielding "is there space?") and managers (capacity planning). *Cost:* turn-aways when space exists, overflow when it doesn't, and zero historical data.
- **No single source of truth links plate ↔ driver ↔ right ↔ event.** When a plate is scanned at the gate, nothing today answers "is this car allowed in, does it have a reserved bay, and is the lot full?" in one place. *Experienced by:* the gate/entry decision point. *Cost:* either everything is allowed (no control) or every entry is a manual judgment call (no throughput).

The platform replaces spreadsheets and tribal knowledge with one authoritative model and a single automated entry-decision at the gate.

---

## 3. User Roles & Personas

Two **staff** roles are mandated: a **System Administrator** for instance-level configuration and a **Parking Operator / Manager** for day-to-day operations, including gate-side exceptions (manual entry/exit, overrides) when LPR fails.  The **Driver** (§3.3) is a separate, non-staff persona — a managed record, not a logged-in role with permissions.

### 3.1 System Administrator — "Aurelia," IT / Facilities Systems Admin
- **Description:** The company's top-level administrator for the instance. Owns global configuration, staff accounts and roles, and the API credentials the gate system uses.
- **Primary goals:** Stand the instance up correctly; manage who-can-do-what; rotate/secure the Access Events API key; keep the system healthy and compliant.
- **Pain points:** Onboarding/offboarding staff, credential sprawl, having to involve the vendor for routine config.
- **Technical proficiency:** High. Comfortable with roles, API keys, and configuration screens.

### 3.2 Parking Operator / Manager — "Marius," Facilities & Parking Manager
- **Description:** Runs day-to-day parking operations for one or more lots, including staffing the entry point when LPR mis-reads or hardware fails and managing walk-up visitors.
- **Primary goals:** Create/edit lots and spaces; grant and revoke driver access (including time-boxed visitor access); assign and reassign reserved bays; monitor live occupancy; reconcile counts when they drift; manually log an entry/exit or override a wrong decision to keep traffic flowing.
- **Pain points:** Keeping access lists current, resolving reserved-bay disputes, not knowing real occupancy, LPR mis-reads, and no fast way to record a manual entry.
- **Technical proficiency:** Moderate. Fluent with web apps and spreadsheets; not a developer.

### 3.3 Driver / End User — "Tomas," Employee Driver (and the Visitor variant)
- **Description:** The person who actually parks. Identified by one or more license plates. **In v1 the driver is a managed record, not a logged-in user** — they neither self-register nor self-book.
- **Primary goals:** Get into the lot they're entitled to; have their reserved bay honored.
- **Pain points:** Being wrongly denied; losing a reserved bay; not knowing if the lot is full (no driver-facing channel exists in v1).
- **Technical proficiency:** N/A for v1 (no direct app interaction). *A future driver portal would target low proficiency.*

> **No driver login in v1**; drivers are admin-managed records only.

---

## 4. Feature List

Priority: **P0** = must-have for v1 launch · **P1** = important, target v1 if time allows · **P2** = nice-to-have / future roadmap.

| Feature | Description | User Role | Priority | Notes |
|---|---|---|---|---|
| Staff authentication (login/logout) | Email + password login for staff users; secure session. | All staff | **P0** | ASP.NET Core Identity recommended. |
| Role-based access control (RBAC) | System Admin / Operator roles gate every action and screen. | System Admin | **P0** | Enforced server-side, not just UI. |
| Password reset (email) | Self-service reset via emailed token. | All staff | **P2** | Not in v1; admin-driven reset only. |
| Staff user management | Create/disable staff accounts, assign roles. | System Admin | **P0** | |
| Access Events API key management | Generate, view-once, and rotate the machine credential used by the gate system. | System Admin | **P0** | Critical security surface. |
| Audit log | Immutable log of access decisions and admin mutations (actor, action, entity, time). | System Admin | **P0** | Needed for ops + GDPR. |
| Parking lot CRUD | Create/edit/archive lots (name, address, timezone). | Operator | **P0** | |
| Lot access mode | Per-lot toggle: **OPEN** (anyone) vs **RESTRICTED** (granted drivers only). | Operator | **P0** | Drives the entry decision. |
| Lot "full" behavior | Per-lot setting: **BLOCK** new general entries once full, or **ALLOW_OVERFLOW** (admit + flag over-capacity). | Operator | **P0** | Reserved holders always bypass. |
| Parking space CRUD | Create/edit/deactivate spaces; set label and type. | Operator | **P0** | |
| Space type (General / Reserved) | Mark a space general or reserved-for-a-driver. | Operator | **P0** | Reserved spaces excluded from general pool. |
| Space placement | Metric position (x/y), rotation, level, bay size and optional zone per space, plus an optional lot footprint — drives the 3D view *without a drag-and-drop builder* (forms + auto-arrange rows). | Operator | **P0** | Pulled into v1 with the 3D view; see §6 + Open Questions Q4. |
| Driver records & plates | Manage drivers; each driver has one or more license plates. | Operator | **P0** | One plate maps to one driver at a time. |
| Access grants | Grant/revoke a driver access to a restricted lot, with optional validity window. | Operator | **P0** | Time-boxing enables visitor access. |
| Reservation management | Assign/reassign/cancel a reserved space for a driver. | Operator | **P0** | One active reservation per space. |
| Access Events ingestion API | Inbound endpoint the LPR/gate calls on ENTER/EXIT; returns ALLOW/DENY synchronously. | (System / API) | **P0** | The single most important external contract. |
| Entry decision engine | Evaluates open/restricted, grants, reservations, and lot-full to allow or deny. | (System) | **P0** | Logic specified in §6. |
| Parking session lifecycle | Open a session on allowed entry; close on exit; never let counts go negative. | (System) | **P0** | Basis of occupancy. |
| Live occupancy view | Per-lot aggregate: general used/free, reserved count, over-capacity flag. | Operator | **P0** | Lot-level only (no per-space sensing). |
| Interactive 3D lot view | Render the lot in 3D from space placements; orbit/zoom/select; color by type/assignment/status; show assignee on a reserved bay; aggregate occupancy overlay. | Operator | **P0** | **v1's primary lot-layout view.** Configuration + aggregate count only, never per-bay occupancy (rule 9). |
| Tabular parking spaces view | Data table of every space (label, type, assignee, status, zone). | Operator | **P0** | Kept as the **accessible equivalent** of the 3D view and the no-WebGL fallback — not the primary view. |
| Manual entry/exit logging | Operator records an entry/exit when LPR fails or for walk-ups. | Operator | **P0** | Audit-logged with actor. |
| Manual override of a decision | Operator overrides a deny/allow at the gate. | Operator | **P0** | Audit-logged with reason. |
| Manual occupancy reconciliation/reset | Operator corrects/zeroes a lot's count to fix drift from missed events. | Operator | **P0** | Mitigates count drift; see §6 rules. |
| Data retention & erasure controls | Configure retention; export/erase a driver's personal data. | System Admin | **P1** | GDPR — see §7 / Open Questions Q1. |
| Auto-expiry of stale sessions | Sessions open beyond a max duration auto-close/expire. | (System) | **P1** | Reduces manual reconciliation. |
| Bulk import of drivers/plates | CSV import for onboarding large lists. | Operator | **P1** | |
| Occupancy history & basic reports | Entries/exits and peak-occupancy over time. | Operator, System Admin | **P1** | |
| Email notifications (access/reservation) | Notify a driver/manager on grant or reservation change. | Operator | **P1** | Requires driver contact data. |
| Multi-lot dashboards | Roll-up occupancy across all of a company's lots. | System Admin | **P1** | |
| Graphical drag-and-drop lot/space builder | Free drag/rotate of bays directly in the 3D scene. | Operator | **P2** | Explicitly **out of scope for v1** — v1 places bays via forms, numeric nudges and auto-arranged rows. |
| Driver self-service portal | Drivers log in to view their access/reservation. | Driver | **P2** | |
| Driver self-booking / reservations | Drivers reserve spaces themselves. | Driver | **P2** | Explicitly out of scope v1. |
| SSO / OIDC / SAML federation | Enterprise identity federation (e.g., Entra ID). | System Admin | **P2** | Likely first enterprise ask post-v1. |
| Per-space occupancy sensing | True per-bay status via sensors/cameras. | (System) | **P2** | Requires hardware the product doesn't have. |
| Waitlists, EV charging, valet, wayfinding | Advanced parking features. | Various | **P2** | |

---

## 5. User Stories by Epic

> Acceptance criteria are provided for **every P0 story**. P1/P2 stories list intent only.

### Epic A — Authentication & Access Control

**A1 (P0).** *As a staff user, I want to log in with my email and password so that only authorized people can use the system.*
- **AC:** Valid credentials create an authenticated session; invalid credentials return a generic "invalid email or password" (no user enumeration); after a configurable number of failed attempts the account is temporarily locked; all traffic is over HTTPS; passwords are stored only as salted hashes.

**A2 (P0).** *As a System Admin, I want roles enforced on every action so that operators can't perform admin-only operations.*
- **AC:** Every API endpoint authorizes by role server-side; a user without the required role receives `403` regardless of UI state; UI hides actions the role can't perform; role checks are covered by automated tests.

**A3 (P2).** *As a staff user, I want to reset a forgotten password so that I can regain access without an admin.* — Not in v1; target future roadmap.
- **AC:** Requesting a reset emails a single-use, time-limited token; the same neutral confirmation shows whether or not the email exists; using the token sets a new password meeting the password policy; the token is invalidated after use or expiry.
- *v1 workaround:* a System Admin resets a staff user's password directly (no self-service, no email token).

**A4 (P0).** *As a System Admin, I want to manage staff accounts and roles so that access reflects the current team.*
- **AC:** Admin can create a staff user with a role, disable a user (immediately ending their sessions), and change a user's role; disabled users cannot authenticate; all changes are audit-logged.

**A5 (P0).** *As a System Admin, I want to generate and rotate the Access Events API key so that the gate system can authenticate and credentials stay secure.*
- **AC:** Admin can generate a key shown **once** at creation (stored hashed thereafter); admin can revoke/rotate a key; revoked keys are rejected immediately; key create/revoke is audit-logged; at least one active key is required for the ingestion endpoint to authorize.

### Epic B — Lot & Space Management

**B1 (P0).** *As an Operator, I want to create and edit parking lots so that the system reflects our physical sites.*
- **AC:** A lot requires a unique name and a timezone; address is optional; archiving a lot hides it from operational views but preserves history; capacity is derived from its active spaces.

**B2 (P0).** *As an Operator, I want to set a lot as open or restricted so that the entry decision matches our access policy.*
- **AC:** Mode is OPEN or RESTRICTED; changing mode takes effect on the next access event; in OPEN mode any plate is admitted (subject to lot-full rules); in RESTRICTED mode only drivers with an active grant or reservation are admitted.

**B3 (P0).** *As an Operator, I want to configure what happens when a lot is full so that overflow behavior is intentional.*
- **AC:** Per-lot setting BLOCK or ALLOW_OVERFLOW; under BLOCK, a general entry into a full lot is denied with reason `LOT_FULL`; under ALLOW_OVERFLOW it is admitted and the lot is flagged over-capacity; reserved-space holders are admitted in both cases.

**B4 (P0).** *As an Operator, I want to create, edit, and deactivate spaces so that the lot's inventory is accurate.*
- **AC:** A space has a label unique within its lot and a type (GENERAL/RESERVED); deactivating a space removes it from capacity and the live view but preserves history; a RESERVED space cannot be deactivated while it has an active reservation (must cancel first).

**B5 (P0).** *As an Operator, I want to set each space's position in the lot so that the 3D view shows the real layout without needing a builder.*
- **AC:** Each space optionally accepts a placement — x/y on the floor plane in metres, rotation, level (for multi-storey sites) and bay width/length (sensible defaults) — plus an optional zone; a lot optionally accepts a footprint (width/length) and level count; placements must fall inside the footprint and on an existing level when those are set; an operator can lay out whole rows at once (auto-arrange: start point, angle, bay count, bay size/angle, aisle gap) and save them in one atomic, audited action; spaces without a placement appear in an "Unplaced" tray rather than breaking the view; overlapping bays produce a warning, not an error.

**B6 (P2).** *As an Operator, I want to drag and rotate bays directly in the 3D view.* — Roadmap (builds on B5's placement data).

### Epic C — Drivers, Plates & Access Grants

**C1 (P0).** *As an Operator, I want to manage driver records and their plates so that the gate can match a scanned plate to a person and their rights.*
- **AC:** A driver has a name and optional contact; a driver can have one or more plates; plate numbers are normalized (case/whitespace) and unique across the instance; a plate can belong to only one driver at a time; reassigning a plate is audit-logged.

**C2 (P0).** *As an Operator, I want to grant or revoke a driver's access to a restricted lot, optionally for a date range, so that employees and visitors get exactly the access they need.*
- **AC:** A grant ties a driver to a lot with optional validFrom/validTo; an expired or revoked grant denies entry to a restricted lot; revocation takes effect on the next access event; grant/revoke is audit-logged; grants have no effect on OPEN lots (which admit anyone).

**C3 (P1).** *As an Operator, I want to bulk-import drivers and plates from CSV.* — Targeted for v1 if time allows.

### Epic D — Reservations

**D1 (P0).** *As an Operator, I want to reserve a specific space for a specific driver so that their bay is documented and honored.*
- **AC:** A reservation ties one space to one driver; **a space may have at most one ACTIVE reservation at a time** — attempting a second is rejected with a clear conflict message; creating a reservation sets the space type to RESERVED and removes it from the general pool; a reserved driver implicitly has access to that lot even if RESTRICTED. Reserving is a single action from the space's row: pick the driver (or pick none, to leave the space unassigned) — no separate screen needed.

**D2 (P0).** *As an Operator, I want to cancel a reservation so that bays follow staffing changes cleanly.*
- **AC:** Cancelling ends the active reservation, freeing the space for a new assignment; all changes are audit-logged with actor.

**D3 (P2).** *As a driver, I want to book a space myself.* — Out of scope v1.

### Epic E — Access Events & Occupancy

**E1 (P0).** *As the gate system, I want to POST an entry event for a plate and receive an allow/deny decision so that the barrier can act immediately.*
- **AC:** Endpoint authenticates via API key; request includes lot, plate, direction=ENTER, event time, and a client idempotency key; response returns ALLOW or DENY plus a machine-readable reason (and, for reserved holders, their space label); duplicate requests with the same idempotency key return the original result without double-counting; median decision latency < 300 ms (see §7).

**E2 (P0).** *As the gate system, I want to POST an exit event so that occupancy is decremented correctly.*
- **AC:** An EXIT closes the matching open session for that plate in that lot (most-recent first); if no open session exists, the event is recorded as an anomaly and occupancy is **not** driven negative; duplicates are deduped by idempotency key.

**E3 (P0).** *As the system, I want to apply the entry decision rules consistently so that access reflects policy.*
- **AC:** Decision precedence is exactly: (1) plate unknown **and** lot RESTRICTED → DENY `NOT_AUTHORIZED`; (2) lot RESTRICTED and no active grant/reservation → DENY `NOT_AUTHORIZED`; (3) reserved-space holder → ALLOW (pool=RESERVED), bypassing lot-full; (4) general entrant and lot full and BLOCK → DENY `LOT_FULL`; (5) otherwise ALLOW (pool=GENERAL). Every decision is persisted with its reason.

**E4 (P0).** *As an Operator, I want to see live occupancy for a lot so that I know if there's room.*
- **AC:** View shows general capacity, general used (= active general sessions), general free (never below zero), reserved space count, and an over-capacity flag; values update on each processed event; the view states clearly that occupancy is lot-level (counted at the gate), not per-space sensed.

**E5 (P0).** *As an Operator, I want to manually log an entry or exit so that LPR failures and walk-ups don't corrupt the count.*
- **AC:** Staff can create a manual ENTER/EXIT for a known plate or an ad-hoc/unknown plate; manual events run through the same session logic; manual events are tagged source=MANUAL with the acting staff user and are audit-logged.

**E6 (P0).** *As an Operator, I want to override an incorrect decision so that a wrongly-denied authorized driver can still get in (and vice-versa).*
- **AC:** An override records the original decision, the overridden decision, the reason, and the actor; an override-to-allow opens a session like a normal allow; overrides are audit-logged and surfaced in reports.

**E7 (P0).** *As an Operator, I want to reconcile or reset a lot's occupancy so that drift from missed exits can be corrected.*
- **AC:** Operator can view active sessions for a lot, close individual stale sessions, or reset the lot to a stated count; every reconciliation action is audit-logged with actor, before/after counts, and an optional note.

**E8 (P1).** *As the system, I want to auto-expire sessions open past a configurable maximum so that drift self-heals.* — Target v1 if time allows.

**E9 (P1).** *As an Operator, I want occupancy history and basic reports.* — Target v1 if time allows.

### Epic F — Visualization & Monitoring

> **v1 scope note (revised):** v1 ships an **interactive 3D lot view (F1)** as the primary way to see a lot's layout. The tabular view (F2) remains as its accessible equivalent and no-WebGL fallback.

**F1 (P0).** *As an Operator, I want an interactive 3D view of the lot so that I can see its layout, which bays are reserved, and to whom.*
- **AC:** Spaces render at their placement (position, rotation, level); the operator can orbit, pan, zoom, switch levels, reset the camera and toggle a top-down view; general, reserved-assigned, reserved-unassigned and inactive spaces are visually distinct (not by color alone); hovering/selecting a space shows its label, type, status, zone and — if reserved — its assigned driver, with reserve/cancel and edit-placement actions available from the selection; unplaced spaces are listed in a tray; a clearly labeled **lot-level, gate-counted** aggregate occupancy indicator is shown and bays are never colored as occupied/free; the view renders ≥ 100 spaces in < 1 s and stays interactive; browsers without WebGL get a message and a link to the table view.

**F2 (P0).** *As an Operator, I want a tabular view of the lot's spaces so that I have an accessible, non-3D way to see which bays are reserved, and to whom.*
- **AC:** A data table lists every space with label, type, assigned driver (if reserved), status, and zone; it is keyboard- and screen-reader-accessible and serves as the documented accessible equivalent of F1.


### Epic G — Audit & Compliance

**G1 (P0).** *As a System Admin, I want an audit log of access decisions and admin changes so that I can investigate incidents and meet obligations.*
- **AC:** Log entries are append-only and capture actor (staff/system/API), action, entity type/id, timestamp; logs are filterable by date, actor, and entity; logs cannot be edited or deleted through the application.

**G2 (P1).** *As a System Admin, I want to configure data retention and to export or erase a driver's personal data so that we can honor data-protection obligations.*
- **AC:** Retention period for access events/sessions is configurable; data past retention is purged or anonymized; an admin can export all personal data for a given driver and can erase/anonymize a driver (removing identifying fields while preserving aggregate counts); erasure and export actions are audit-logged. *(Not legal advice — see Open Questions Q1.)*

---

## 6. Data Model Overview

Plain-English entities, core attributes, and relationships. **PostgreSQL** with **EF Core** is the assumed persistence layer.

### Entities

- **OrganizationSettings** (effectively a singleton per instance): company name, branding, default timezone, default lot-full behavior, data-retention period. *Because deployment is single-tenant, there is no cross-company tenancy inside one instance.*
- **StaffUser** (logs in): id, name, email (unique), password hash, role (`SystemAdmin` | `Operator`), status (active/disabled), timestamps. *(Backed by ASP.NET Core Identity.)*
- **Driver** (does **not** log in v1): id, name, optional contact (email/phone), status, timestamps.
- **Plate**: id, driver id, normalized plate number (unique across instance), optional country/region, active flag.
- **ParkingLot**: id, name (unique), optional address, timezone, access mode (`OPEN` | `RESTRICTED`), full behavior (`BLOCK` | `ALLOW_OVERFLOW`), status (active/archived), optional layout (footprint width/length in metres, level count).
- **ParkingSpace**: id, lot id, label (unique within lot), type (`GENERAL` | `RESERVED`), optional zone, optional placement (x, y in metres on the lot floor plane, rotation in degrees, level, bay width/length), status (active/inactive).
- **AccessGrant**: id, driver id, lot id, valid-from, optional valid-to, status, created-by, created-at. *(Only meaningful for RESTRICTED lots.)*
- **Reservation**: id, space id, driver id, lot id (denormalized), status (`ACTIVE` | `CANCELLED`). *(No start/end date, no created-by/created-at on the entity itself — creation and cancellation are captured in the audit log.)*
- **AccessEvent**: id, lot id, raw plate string, matched plate id (nullable), matched driver id (nullable), direction (`ENTER` | `EXIT`), source (`LPR` | `MANUAL`), decision (`ALLOW` | `DENY`), deny reason (nullable), occurred-at, received-at, idempotency key (unique), acting staff id (if manual/override), session id (nullable), override-of (nullable).
- **ParkingSession**: id, lot id, driver id (nullable for unknown plates in OPEN lots), plate string, space id (set only for reserved-pool sessions), pool (`GENERAL` | `RESERVED`), entry event id, entry time, exit event id (nullable), exit time (nullable), status (`ACTIVE` | `CLOSED` | `EXPIRED`).
- **AuditLogEntry**: id, actor type (`STAFF` | `SYSTEM` | `API`), actor id (nullable), action, entity type, entity id, timestamp, metadata (JSON).

### Relationships (plain English)
- A **Driver** has **many Plates**; a **Plate** belongs to exactly **one Driver** at a time.
- A **ParkingLot** has **many ParkingSpaces**; a lot's capacity is the count of its **active** spaces.
- A **ParkingSpace** may have **many Reservations over time** but at most **one ACTIVE** reservation.
- A **Reservation** ties **one Space** to **one Driver**.
- An **AccessGrant** ties **one Driver** to **one Lot** for an optional time window.
- An **AccessEvent** belongs to **one Lot** and optionally resolves to a Plate/Driver; an ENTER that is allowed **opens one ParkingSession**, and an EXIT **closes** it.
- A **ParkingSession** belongs to **one Lot**, optionally to a **Driver** and a **Space** (reserved pool only).

### Business rules (called out explicitly)
1. **A parking space can have at most one ACTIVE reservation at a time.** A second attempt is rejected as a conflict.
2. **A reserved space is excluded from the general availability pool** and is held for its assigned driver regardless of demand.
3. **General free = general capacity − active GENERAL sessions, floored at zero** (it never goes negative; overflow is represented by the over-capacity flag, not negative free space).
4. **Entry decision precedence** (the single source of truth for the gate): unknown plate in a RESTRICTED lot → deny; RESTRICTED lot with no active grant/reservation → deny; **reserved-space holder → allow and bypass lot-full**; general entrant into a full lot under BLOCK → deny `LOT_FULL`; otherwise allow.
5. **A reserved-space holder entering occupies their reserved space** (pool = RESERVED) and does **not** consume a general slot.
6. **EXIT closes the matching open session** for that plate/lot, most-recent first. If none exists, record an anomaly; **never drive occupancy negative.**
7. **Events are idempotent** by client-supplied idempotency key; replays return the original outcome and never double-count.
8. **One plate maps to one driver** at a time; reassignment is explicit and audited.
9. **Occupancy is lot-level only.** Because detection is gate-counted (no per-space sensors), the system does **not** know which physical bay a car occupies; the 3D lot view (and its tabular equivalent) shows *configuration* (layout, types, reserved-bay assignees) plus an *aggregate* count, not live per-space status — bays are never colored occupied/free.
10. **Count drift is expected** (a missed exit leaves a stale session). Mitigations: manual reconciliation/reset (P0) and configurable auto-expiry of long-open sessions (P1).
11. **Access decisions and all admin mutations are audit-logged.**
12. **Retention & erasure:** access events/sessions are retained only for the configured period; a driver's personal data can be exported and erased/anonymized while preserving aggregate counts.

---

## 7. Non-Functional Requirements

### Availability & uptime
- **Define a gate fail-safe** for when the platform is unreachable — barrier defaults to *open* or *closed*. This is a safety/operations decision the product must specify per deployment (see Open Questions Q2); the platform itself remains stateless enough to restart quickly.
- Single-tenant deployment means HA requires **redundant API replicas + managed/replicated PostgreSQL**; absent that, recovery is restart-based. Confirm per-customer SLA (Open Questions Q8).

### Security
- **Auth:** ASP.NET Core Identity with RBAC; salted password hashing; HTTPS/TLS 1.2+ everywhere.
- **Machine auth:** Access Events API authenticated by API key (hashed at rest, view-once, rotatable); rate-limited; **OAuth client-credentials is the more robust alternative** (Open Questions Q5).
- **Data protection:** PostgreSQL at-rest encryption (disk/volume), parameterized queries via EF Core (no string-built SQL), and consideration of column-level protection for plate data.
- **Input validation:** server-side validation on every endpoint (FluentValidation recommended); reject malformed plates/events.
- **Audit & abuse controls:** append-only audit log; rate limiting on auth and ingestion endpoints; secrets never logged.

### Scalability
- **Per-tenant load is modest** (≈1 lot, 20–100 spaces; event volume proportional to lot size — tens of events/minute at peak for a large lot). Vertical sizing comfortably covers a typical tenant.
- **The real scaling axis is the number of tenant instances**, not per-instance load — so repeatable provisioning matters more than per-instance horizontal scale.

### Testing
- **Target ~30% unit test coverage** across the backend solution (not per-file, not CI-enforced) — a rough goalpost, not a gate. Prioritize the highest-risk logic first: **`EntryDecisionService`** (rule-4 precedence, E3) and **occupancy math** (rule 3) should be covered well above that, given they're the product's core IP (§1 driver 2); command/query handlers and validators get whatever's left of the budget.
- **Tooling:** `xUnit` + `Shouldly` (assertions) + `NSubstitute` (mocking) for unit tests; `coverlet.collector` + `ReportGenerator` to measure coverage when useful — all already centrally versioned in `Directory.Packages.props` (backend template default), just not yet wired into a test project. See architecture §3 for the proposed `Parkin.UnitTests` / `Parkin.IntegrationTests` / `Parkin.FunctionalTests` split.

---

## 8. Technical Constraints & Decisions

### Decided (do not re-litigate)
- **Frontend:** React + TypeScript (SPA).
- **Backend:** .NET 10.
- **Database:** PostgreSQL.
- **Deployment:** **Single-tenant — one instance and one database per client company.** No multi-tenant data sharing inside an instance.
- **No payments anywhere** in the product.
- **No driver self-registration or self-booking in v1.** Access grants and reservations are admin-created.
- **No drag-and-drop lot/space builder in v1**; spaces are created via forms and positioned via placement fields, numeric nudges and auto-arranged rows.
- **Occupancy is gate-counted via an inbound Access Events API.** The product does **not** include or control LPR cameras or barrier hardware; it ingests their events and returns decisions.
- **No third-party integrations to build** — with one unavoidable consequence below.
- **~30% unit test coverage target** using xUnit/Shouldly/NSubstitute (§7 Testing).

### Recommended defaults (proceed unless a customer objects)
- **Auth:** ASP.NET Core Identity + RBAC; **prefer same-site cookie auth** for the first-party SPA (simpler, safer against token theft) — JWT is the alternative if a non-browser client later appears (Open Questions Q6).
- **ORM:** EF Core with code-first migrations.
- **Validation:** FluentValidation on all inbound DTOs.
- **Machine credential:** API key (hashed, rotatable) for the Access Events endpoint in v1.
- **Packaging & provisioning:** containerized (Docker) per tenant, stood up via Infrastructure-as-Code so that "one instance per company" is repeatable and upgradable (Open Questions Q8).
- **Layout data:** free-form metric placement per space (x/y, rotation, level, bay size) + optional zone and lot footprint; rows generated client-side by an auto-arrange tool (Open Questions Q4).
- **3D rendering:** three.js via React Three Fiber (+ drei helpers), lazy-loaded on the lot-view route only.

### Open architectural decisions
Cloud/hosting provider; gate fail-safe behavior; cookie-vs-JWT auth; tenant provisioning/upgrade tooling; future SSO. Each is itemized in §10 with a recommended default so engineering is not blocked.

---

## 9. Out of Scope (v1)

This system will **not**, in v1:

1. **Process any payment, fee, fine, billing, or refund.** No money flows through the product at all.
2. **Let drivers self-register, self-book, or self-reserve.** All grants and reservations are created by staff.
3. **Provide a drag-and-drop lot/space builder.** Spaces are configured via forms, placement fields and auto-arranged rows.
4. **Provide a driver-facing app or portal** (web or mobile). Drivers are managed records, not users.
5. **Provide photorealistic/CAD-grade lot modeling** (imported floor plans, terrain, ramps as geometry, custom 3D assets). The v1 3D view renders simple bays on flat level slabs.
6. **Detect per-space occupancy.** No bay-level sensors/cameras; occupancy is lot-level, counted at the gate. The v1 3D view shows configuration + an aggregate count, not live per-bay status.
7. **Include or control LPR/ANPR cameras or barrier-gate hardware**, or run plate-recognition itself. It only ingests events and returns decisions.
8. **Be multi-tenant within one instance.** One deployment serves one company.
9. **Provide SSO / SAML / OIDC federation** with corporate identity providers.
10. **Provide navigation, wayfinding, or turn-by-turn routing** to a space.
11. **Send driver/occupancy notifications** (email/SMS/push) beyond transactional staff emails (password reset). Access/reservation notifications are P1.
12. **Provide advanced analytics/BI dashboards** beyond the live occupancy view (basic history is P1).
13. **Support waitlists, dynamic pricing, EV-charging management, valet, or visitor self-service kiosks.**
13. **Integrate with any existing internal customer system** (HR, badging, ERP, etc.).

---

## 10. Open Questions

Each item has a recommended default so work can proceed; confirm before the noted milestone.

| # | Question | Recommended default | Suggested owner | Rough deadline |
|---|---|---|---|---|
| Q1 | **GDPR almost certainly applies.** Storing license plates + entry/exit timestamps + identity is processing personal data of (presumably EU/Lithuanian) data subjects. What is the lawful basis, retention period, and erasure/export process? *(This PRD bakes in retention + erasure controls; this is not legal advice.)* | Treat GDPR as **in scope**: define lawful basis, default 90-day event retention, and erasure/anonymization. | Product + Legal/DPO | **Before storing real plate data / before beta** |
| Q2 | **Gate fail-safe:** when the platform is unreachable, should the barrier default **open** or **closed**? Safety, liability, and security all hinge on this. | Decide per deployment; document explicitly. Lean **closed for restricted lots, open for open lots** unless customer dictates otherwise. | Product + customer security/ops | Before integration testing |
| Q3 | **Occupancy semantics for the 3D lot view.** Confirm that v1 shows lot-level aggregate + static per-space config (not live per-bay status), since detection is gate-counted only. | Confirm aggregate-only; label it clearly in the UI; never color bays occupied/free. | Product | Before the lot-view sprint |
| Q4 | **Layout data without a builder.** How do spaces get their positions — row/column ordinals, free x/y coordinates, or auto-grid? | **Resolved: free-form metric x/y + rotation + level (+ bay size, zone) per space**, because a 3D scene needs real geometry (angled bays, aisles, levels). Grid convenience comes from a client-side auto-arrange rows tool that emits x/y; drag-and-drop stays P2 (B6). | Eng + Design | Resolved 2026-09-25 |
| Q5 | **Access Events API contract:** confirm the platform *exposes* the endpoint and the gate *calls in*; define request/response schema, idempotency, error codes, and auth (API key vs OAuth client-credentials). | Platform exposes a synchronous `POST /access-events`; **API key** for v1, idempotency key required, ALLOW/DENY + reason in response. | Eng (backend) | Before the API spec is finalized |
| Q6 | **SPA auth token strategy:** same-site cookies vs JWT bearer. | **Same-site, HttpOnly cookies** for the first-party SPA; revisit JWT if a non-browser client appears. | Eng (backend) | Before auth implementation |
| Q7 | **Driver login in v1?** Do drivers get any portal, or are they purely admin-managed records? This sizes the auth surface. | **No driver login in v1** (records only). Driver portal is P2. | Product | Before auth design |
| Q8 | **Tenant provisioning & upgrades:** how are per-company instances/databases created, upgraded, and operated, and by whom (vendor ops)? | **Docker + IaC, DB-per-tenant**, vendor-operated; define an upgrade runbook. | DevOps/Platform | Before infra build |
| Q9 | **Peak concurrency / event throughput per tenant** was not provided. Confirm assumptions so NFRs can be load-tested. | Assume tens of concurrent staff sessions and event volume proportional to lot size (≤ a few/min even for 100 spaces). | Product | Before NFR sign-off |
| Q10 | **Multiplicity rules:** confirm a driver may hold **multiple plates** (assumed yes) and whether a driver may hold **more than one reserved space** (per lot / overall). | Multiple plates per driver: **yes.** Reserved spaces per driver: **at most one per lot** in v1. | Product | Before data-model freeze |
| Q11 | **Lot-full behavior nuance:** confirm reserved holders always bypass the full check and that general overflow is a per-lot choice. | Reserved holders **always bypass**; overflow is per-lot `BLOCK`/`ALLOW_OVERFLOW`. | Product | Before decision-engine build |
| Q12 | **SSO demand:** how soon will enterprise customers require identity federation (Entra ID/SAML)? | Plan for **post-v1 (P2)**; keep Identity abstraction clean to add later. | Product/Sales | Roadmap planning |

---

*End of document — v0.1 draft for review.*
