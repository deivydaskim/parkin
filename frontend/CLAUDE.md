# Frontend (operator console)

Vite + React 19 + TS SPA: TanStack Router/Query/Table, Tailwind v4 + shadcn/ui, axios, Zod v4, react-hook-form, Zustand, react-three-fiber for the 3D lot view. Use the `@/` alias for `src/`.

## Commands (pnpm, run from `frontend/`)

```powershell
pnpm dev            # Vite on :5173 (strictPort), expects the API at VITE_API_BASE_URL
pnpm build          # vite build && tsc -b; this is the only typecheck
pnpm lint           # eslint .
pnpm format         # prettier --write . (a PostToolUse hook already formats edited files)
pnpm test           # vitest run
pnpm vitest run src/features/lot-view/geometry.test.ts
pnpm exec shadcn add <component>        # adds primitives to src/components/ui (components.json, new-york style)
```

- Copy `.env.example` to `.env` and set `VITE_API_BASE_URL` (default `http://localhost:5000`). `src/config/env.ts` parses `import.meta.env` with Zod at startup, so a missing or invalid value crashes the app on load. Add any new `VITE_*` variable to that schema.
- Tests are plain Vitest with no DOM environment and currently cover only pure logic (`features/lot-view/geometry.ts`, `auto-arrange.ts`). Put tests next to the file as `*.test.ts`.
- Prettier: no semicolons, single quotes, trailing commas.
- The API must be running with CORS allowing `http://localhost:5173` because auth is a cookie (`withCredentials`).

## App wiring

- `main.tsx` creates the router with `context: { queryClient }`. `routes/__root.tsx` wraps everything in `app/providers.tsx` (next-themes, QueryClientProvider, TooltipProvider, sonner `Toaster`, React Query devtools).
- `app/query-client.ts` defaults: `retry: 1`, `staleTime: 30s`, `refetchOnWindowFocus: false`. Live screens (occupancy, active sessions, gate access events) poll with `refetchInterval` constants defined in their `queries.ts`.
- `vite.config.ts` uses `tanstackRouter({ autoCodeSplitting: true })`, so each route file is split automatically. IMPORTANT: never edit `routeTree.gen.ts`; the dev server or the build regenerates it.
- `lib/api-client.ts` is axios with `withCredentials` (cookie auth). A 401 on a non-auth endpoint hard-redirects to `/login`.

## Routing conventions

- `routes/_authenticated/route.tsx` `beforeLoad` runs `queryClient.ensureQueryData(currentUserQueryOptions)`, mirrors the user into the Zustand store (`features/auth/store.ts`), and redirects to `/login?redirect=...` on failure. The Query cache stays the source of truth; the store exists so `useHasRole`/`RoleGate` can read the user synchronously.
- Breadcrumbs (`components/layout/Breadcrumbs.tsx`) use the deepest match with `staticData.crumb`. `crumb` is a string or a component (e.g. `DriverCrumb`, `LotCrumb`, which read params with `useParams({ strict: false })` and load the entity name via its query). Ancestors come from `staticData.parentCrumbs`. The typing is augmented in `app/route-meta.ts`.
- `lots/$lotId_.map.tsx` uses a trailing `_` to un-nest the 3D map from the lot detail layout.
- List pages keep filters, pagination, tabs and even dialog state (`?create=true`) in the URL. Each search field is `z....optional().catch(undefined)` so bad params fall back to defaults, defaults are applied when destructuring `Route.useSearch()`, and default values are written back as `undefined` to keep URLs clean. See `routes/_authenticated/drivers/index.tsx` as the reference list page.
- ESLint turns off `react-refresh/only-export-components` for `src/routes/**` (they export `Route`) and `src/components/ui/**`.

## Feature slice conventions (`src/features/<area>/`)

- Each area has `schemas.ts`, `api.ts`, `queries.ts` and `components/`. One Zod schema serves as both the react-hook-form resolver and the API response parser.
- `schemas.ts`: Zod v4 (`z.uuid()`, `z.url()`). Each area usually has the resource schema (API response), a `*FormSchema` for react-hook-form, `*ListParamsSchema` (camelCase `perPage`), and `*ListResponseSchema` (`items`, `page`, `perPage`, `totalCount`, `totalPages`). Enums are exported both as a type and as a value: `export const DriverStatus = driverStatusSchema.enum`.
- `api.ts`: functions over the shared `apiClient`. Translate params to the API's snake_case (`per_page`), send `search: value || undefined`, and always `schema.parse(data)` the response.
- `queries.ts`: `xxxQueryOptions(...)` factories (lists use `placeholderData: keepPreviousData`, details use `enabled: !!id`), thin `useXxx` hooks, and one mutation hook per action. On success they invalidate `qk` keys and `setQueryData` the detail with the returned entity, then `toast.success`. On error they `toast.error(parseApiError(error, 'Could not ...').message)`. Mutations that affect other areas also invalidate those areas' keys (e.g. plate reassign invalidates both drivers).
- New query keys go in `src/lib/query-keys.ts`. Invalidating `qk.x.list()` with no params matches every param variant, because the key prefix is `['x', 'list']`.
- Optional extras seen in slices: `labels.ts` (enum → display text), `form-values.ts` (entity → form defaults, e.g. `lotToFormValues`), `components/*Crumb.tsx`.
- `parseApiError` (`lib/api-error.ts`) reads RFC 7807 ProblemDetails and picks `detail`, then the first validation error, then `title`, then the fallback.

## Notable areas

- `features/gate`: the operator's in-console gate (manual entry/exit through `POST /lots/{lotId}/manual-events`). It is not the separate `gate/` simulator app. The mutation generates one `Idempotency-Key` (`crypto.randomUUID()`) per distinct form input and reuses it on retries of the same input, so a retry replays the same decision on the server. `decision.ts` maps deny reasons to labels.
- `features/lot-view`: react-three-fiber / drei 3D lot map. Pure math lives in `geometry.ts`, `auto-arrange.ts` and `scene-model.ts` (with light/dark `ScenePalette`s, because three.js colors can't use CSS tokens). React components under `components/` only render. `LotView` checks `isWebGLAvailable()` and falls back when WebGL is missing. Keep new layout logic in the pure modules so it stays unit-testable.
- `features/auth`: `RoleGate`, `useHasRole(...roles)` and `hasRole(user, roles)` (`permissions.ts`). Roles are `Operator` and `SystemAdmin`. This only hides UI; the server enforces authorization, so a new admin-only action also needs `Roles(...)` on its endpoint.

## UI building blocks

- Type React props as `type Props = {...}`, not `interface FooProps`.
- shadcn primitives live in `src/components/ui/`. Before writing a new page component, check `src/components/` for `PageHeader`, `EmptyState`, `DataTablePagination`, `SearchInput` (debounced), `ListSkeleton`/`Skeletons`, `ConfirmDialog`, `EntityCombobox`, `StatusBadge`, `PlateChip`, `StatCard` and `OccupancyBar`. Layout chrome is in `components/layout/`.
- Tailwind v4 with no config file. Tokens are CSS variables in `src/styles/globals.css`, exposed through `@theme inline`, and include the custom `success`, `warning` and `info` colors besides the shadcn set. Dark mode is class based (`.dark`, toggled by next-themes).
- Use `cn()` from `@/lib/utils` to merge classes, and `lucide-react` for icons.
