---
name: plan-next-task
description: Start planning the next V1 MVP task. Reads V1-MVP-TASKS.md, finds the first unchecked task (`- [ ]`), loads its dependencies/deliverables/AC and the referenced patterns, then produces an implementation plan for it. Use when the user says "plan the next task", "what's next in the MVP", "/plan-next-task", or wants to begin the next V1-MVP build step.
user-invocable: true
---

# Plan the next V1 MVP task

Drive the [`V1-MVP-TASKS.md`](../../../V1-MVP-TASKS.md) checklist top-to-bottom. This skill picks the **first uncompleted task** and produces a ready-to-execute implementation plan.

## Steps

1. **Read** `V1-MVP-TASKS.md` (repo root).

2. **Find the target task** = the first line matching `- [ ]` (unchecked), scanning top to bottom. That order is dependency-respecting by design — do not skip ahead.
   - If an argument names a task ID (e.g. `T5.2`), plan that one instead.
   - If every task is `- [x]`, report "V1 MVP complete — no unchecked tasks" and stop.

3. **Verify dependencies.** Parse the task's `*Deps: ...*`. For each dep task ID, confirm it is `- [x]`. If any dep is still unchecked, warn the user: this task is blocked, and the real next task is the blocking dep. Ask whether to plan the dep instead or proceed anyway.

4. **Load context** for the target task:
   - Its **deliverables**, **AC**, and the **patterns/files** named in the task line and in the "Global conventions" block at the top of the doc.
   - The source-of-truth docs for the story: [`parking-management-system-prd.md`](../../../parking-management-system-prd.md) (find the matching story, e.g. `A1`, `E3`) and [`parking-management-system-architecture.md`](../../../parking-management-system-architecture.md) (domain model, invariants, decision path).
   - Relevant existing code patterns in `backend/src/Parkin.Api/` (the Product/Cart/Order slices as worked examples). Use the [`new-aggregate`](../new-aggregate/SKILL.md) and [`create-migration`](../create-migration/SKILL.md) skills when the task adds an aggregate or migration.

5. **Enter plan mode** (`EnterPlanMode`) and produce the plan. The plan must cover, in order:
   - **Backend**: aggregate/entity + Vogen ID + VogenEfCoreConverters registration + EF config + DbSet + migration; feature slice(s) (endpoint/request/response/validator/[mapper], handler if cross-aggregate); NsDepCop rule additions.
   - **Frontend**: `src/features/<x>/` folder, route under `src/routes/_authenticated/`, Zod schema (RHF resolver + response parser), regenerate `src/types/api.ts`.
   - **Tests** weighted by the task's risk (units for pure domain services, integration for DB invariants/idempotency, RbacTests for role/endpoint pairs).
   - **Verify step**: the exact real flow to drive per the "How to run / verify" section (use `/verify`).
   - Each step maps to one AC — the plan is done when every AC is covered.

6. On `ExitPlanMode` approval, implement. When the task lands and its AC pass, **check it off** (`- [ ]` → `- [x]`) in `V1-MVP-TASKS.md`.

## Conventions to enforce (from CLAUDE.md)

- Backend = vertical slices via FastEndpoints (REPR); Mediator = martinothamar (`ValueTask`, `ICommandHandler`/`IQueryHandler`), **not** MediatR.
- DB = PostgreSQL (Npgsql). Guid IDs for parking aggregates (per T0.2).
- `dotnet build` must stay clean (`TreatWarningsAsErrors=true`, NsDepCop `NSDEPCOP01` = error). Run backend commands from `backend/`.
