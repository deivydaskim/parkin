# Gate simulator

Demo of a physical plate-reader gate (Vite + React 19, Zustand, Tailwind v4 + shadcn/ui). It calls the real `POST /api/v1/access-events` with `X-Api-Key` and `Idempotency-Key`, shows the decision and opens or holds the barrier. It needs no backend changes; `README.md` has the demo script.

## Commands (pnpm, run from `gate/`)

```powershell
pnpm gate:setup     # idempotent dev setup: logs in as dev admin, creates a lot-scoped API key, writes .env.local
pnpm dev            # :5174 (strictPort)
pnpm build          # vite build && tsc -b (this is the typecheck)
pnpm lint
pnpm format         # the repo's Prettier hook only covers frontend/, so run this after editing here
pnpm test           # vitest run
```

## Key rules

- IMPORTANT: the API key must never reach the browser. `vite.config.ts` proxies `/api/v1/*` to `PARKIN_API_URL` and injects `GATE_API_KEY` server side. Never expose it through a `VITE_*` variable or call the API directly from the client.
- Only `VITE_*` values are client-visible, and `src/config/env.ts` validates them with Zod (`VITE_GATE_LOT_ID`, `VITE_GATE_LOT_NAME`, `VITE_DEMO_PLATES`). Add new client env vars to that schema and to `.env.example`.
- The API call and response schemas live in `src/lib/gate-api.ts` and `src/lib/schemas.ts`. Keep them in sync with the backend's access-event contract.
- Pure logic (`lib/plates.ts`, `lib/decision-messages.ts`, `store/gate-store.ts`) has colocated `*.test.ts`. Keep new logic in testable modules, not in components.
