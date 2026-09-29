# Task: Scope gate API keys to a parking lot

## Problem

`ApiKey` is not tied to any parking lot. The gate sends `LotId` in the body of `POST /api/v1/access-events`, and `IngestAccessEventHandler` trusts it. Any valid key can therefore post events for any lot.

Consequences:
- A key leaked from Lot A can open and close sessions in Lot B and exhaust its capacity.
- A wrong `lotId` in a gate's config silently creates sessions in the wrong lot.
- Events record only the key as actor, so they cannot be traced to a physical gate, and one gate cannot be revoked without guessing which key it used.

## Goal

The lot is a server-side property of the credential. The gate no longer decides which lot it acts on.

## Deliverables

### Backend
- `ApiKey` gets a required `ParkingLotId LotId` (FK to `parking_lots`), set in `ApiKey.Create(name, lotId, createdByUserId)`.
- EF configuration and migration. Existing keys have no lot: revoke them, or backfill if a single lot exists.
- `ApiKeyAuthenticationHandler` adds a `LotId` claim for the authenticated key.
- `IngestAccessEventEndpoint` takes the lot from the claim. `LotId` is removed from the request body, or, if kept for compatibility, a mismatch with the claim returns 403.
- `CreateApiKeyEndpoint` request takes `LotId`, validates that the lot exists, and rejects archived lots.
- List/DTO/record for API keys expose the lot id and name.
- Audit entries for key creation include the lot.
- `RecordManual` (staff) is unchanged and keeps taking the lot in the body.
- Optional: a gate identifier (for example `GateName`) on the key, stored on `AccessEvent` for traceability.

### Frontend
- API key create form has a required lot selector (`EntityCombobox`).
- API key list shows the lot column.

### Gate simulator
- `pnpm gate:setup` creates a key bound to the demo lot.
- The simulator stops sending `lotId` (or sends the matching one).

## Acceptance criteria
- Creating a key without a lot returns 400; with an unknown or archived lot returns 400/404.
- An ingest call with a Lot A key records the event against Lot A regardless of any lot id in the body.
- An ingest call whose body lot differs from the key's lot returns 403 (if the field is kept).
- Revoked keys still return 401.
- Unit, integration and functional tests cover the above; `dotnet build` passes with `TreatWarningsAsErrors`.

## Open questions
- Should any key be system-wide (nullable `LotId`), or is the lot always mandatory? Current recommendation: mandatory.
- Is the per-gate identifier in scope now or a follow-up?
