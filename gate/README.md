# Parkin Gate Simulator

A demo of the physical plate-reader gate. It reads a plate, calls the real inbound API
(`POST /api/v1/access-events` with `X-Api-Key` and `Idempotency-Key`), gets a decision back
and opens or keeps closed the barrier. No backend changes are needed.

The API key never reaches the browser: the Vite dev/preview server proxies `/api/v1/*` to
`PARKIN_API_URL` and adds the `X-Api-Key` header server side, like firmware holding a device secret.

## Setup

1. Start the backend from `backend/`:

   ```powershell
   dotnet run --project src/Parkin.AspireHost
   ```

2. From `gate/`:

   ```powershell
   pnpm install
   pnpm gate:setup
   pnpm dev
   ```

3. Open <http://localhost:5174>.

`pnpm gate:setup` is idempotent and dev-only. It logs in as the dev admin, finds the lot,
creates an API key named "Gate simulator" (unless `.env.local` already holds an active one),
gives the seeded reservation drivers demo plates if they have none, and writes `.env.local`.

| Option / env var                               | Purpose                                            |
| ---------------------------------------------- | -------------------------------------------------- |
| `pnpm gate:setup --lot "<name>"`               | Use another lot (default `Demo Parking - Central`) |
| `PARKIN_API_URL`                               | API base URL (default `http://localhost:5000`)     |
| `PARKIN_ADMIN_EMAIL` / `PARKIN_ADMIN_PASSWORD` | Override the dev admin credentials                 |

## Demo script

- **Reserved entry**: pick `ONA-001` in the Entry lane, Drive in. Allow, "Reserved space A13".
- **General entry**: Random visitor, Drive in. Allow, "Welcome".
- **Exit**: pick the plate from "Inside" in the Exit lane, Drive out. Allow; it leaves the list.
- **Anomaly**: Unknown plate in the Exit lane. Deny, `NoOpenSession`, "No entry recorded".
- **Idempotency**: "Resend last event" repeats the last request with the same `Idempotency-Key`.
  The log marks it as a replay and shows the decision is identical.
- **Fail-safe**: stop the API. The lane shows "Gate offline"; the toggle decides whether the
  barrier is released (fail-open) or stays locked (fail-closed). Offline events are not recorded
  by Parkin, so the "vehicles inside" list is left unchanged.
- **Busy gate**: switch on Auto traffic for random arrivals and departures.
- **Contract**: expand any API log row to see the exact request (key masked) and response JSON.
- **Restricted lot**: set the lot to Restricted in the operator console; an unregistered plate
  gets Deny, `NotAuthorized`.

Parkin has no "already inside" check, so a second Enter for the same plate opens a second
session. The simulator warns about it but still lets the car through.

## Scripts

```powershell
pnpm dev     # simulator on :5174
pnpm build
pnpm lint
pnpm test
```
