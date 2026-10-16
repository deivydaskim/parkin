# Parkin

Single-tenant Parking Management System. Monorepo with three independent apps. Each has its own `CLAUDE.md` with commands and conventions, which Claude Code loads when working in that directory:

| App | Stack | Port | Details |
| --- | --- | --- | --- |
| `backend/` | .NET 10 API (`Parkin.slnx`), FastEndpoints, EF Core + PostgreSQL, Aspire | `:5000` / `:5001` | `backend/CLAUDE.md` |
| `frontend/` | Operator console SPA: Vite, React 19, TS, TanStack Router/Query, Tailwind v4 + shadcn/ui | `:5173` | `frontend/CLAUDE.md` |
| `gate/` | Plate-reader gate simulator: Vite, React 19, Zustand | `:5174` | `gate/CLAUDE.md` |

The backend owns lots/spaces, drivers/plates, access grants, reservations, parking sessions, the audit log, and the inbound Access Events API that decides whether a vehicle may enter.

## Working across apps

- There is no root workspace. Run `dotnet` commands from `backend/` and `pnpm` commands from `frontend/` or `gate/`. Each JS app has its own `node_modules` and lockfile.
- Local stack: start the API with `dotnet run --project src/Parkin.AspireHost` (Docker must be running), then `pnpm dev` in `frontend/` and/or `gate/`.
- Dev logins (seeded on API startup): `admin@parkin.local` / `Admin!2345` (SystemAdmin), `operator@parkin.local` / `Operator!2345` (Operator). Demo lot: `Demo Parking - Central`.
- Two auth schemes cross the app boundary:
  - **Staff (frontend)**: ASP.NET Identity cookie, so the SPA uses `withCredentials` and the API must allow its origin in `Cors:AllowedOrigins` (default `http://localhost:5173`).
  - **Gates (gate app)**: `X-Api-Key` scoped to one parking lot. The gate's Vite server proxies `/api/v1/*` and adds the key server side, so the key never reaches the browser.
- API contract changes are not shared automatically. When a backend request/response DTO changes, update the matching Zod schema in `frontend/src/features/<area>/schemas.ts` (and `gate/src/lib/schemas.ts` for access events). The frontend parses every response with Zod, so a mismatch fails at runtime, not at build time.
- Wire format: JSON is camelCase, enums are strings, list query params are snake_case (`page`, `per_page`), and errors are RFC 7807 ProblemDetails.

## Before finishing a change

Run the checks for every app you touched:

- `backend/`: `dotnet build` (warnings and NsDepCop violations fail it) and the relevant test project.
- `frontend/` or `gate/`: `pnpm build` (this is the typecheck), `pnpm lint`, `pnpm test`.
