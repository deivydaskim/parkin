---
name: new-aggregate
description: Conventions and the complete checklist for adding a new domain aggregate (or entity) to the Parkin.Api backend — EntityBase + IAggregateRoot, Vogen strongly-typed IDs, the mandatory VogenEfCoreConverters registration, EF IEntityTypeConfiguration, and the NsDepCop boundary rules. Use whenever creating a parking-domain aggregate such as ParkingLot, ParkingSpace, Driver, AccessGrant, Reservation, or ParkingSession.
user-invocable: false
---

# Adding a new aggregate to Parkin.Api

Follow this checklist end-to-end. The two steps that fail silently — the **Vogen converter registration** and the **EF value generator** — are called out with ⚠️. All paths are under `backend/src/Parkin.Api/`.

## 1. Strongly-typed ID (Vogen)

Create `Domain/<Name>Aggregate/<Name>Id.cs`. Mirror `Domain/ProductAggregate/ProductId.cs`:

```csharp
using Vogen;

namespace Parkin.Api.Domain.<Name>Aggregate;

[ValueObject<int>]                       // or [ValueObject<Guid>] for gate/externally-visible ids
public readonly partial struct <Name>Id
{
  // Int-keyed sentinel meaning "not yet persisted"; EF assigns the real value on SaveChanges.
  public static <Name>Id New => From(0);

  private static Validation Validate(int value)
      => value >= 0 ? Validation.Ok : Validation.Invalid("<Name>Id must be non-negative.");
}
```

- **Int IDs** use the `New => From(0)` sentinel + a value generator (step 4). Use for internal aggregates.
- **Guid IDs** omit the `From(0)` sentinel; generate the Guid in `Create(...)`. Prefer for entities the gate/API references by a stable external key. Any extra value object (like `Price`/`Quantity`) follows the same `[ValueObject<T>]` pattern.

## 2. Aggregate root

Create `Domain/<Name>Aggregate/<Name>.cs`. Mirror `Domain/ProductAggregate/Product.cs`:

- Derive from `EntityBase<<Name>, <Name>Id>` and implement `IAggregateRoot`.
- A **private parameterless constructor** for EF Core.
- A constructor that `Guard.Against.InvalidInput(id, ..., id => id != <Name>Id.New, "Use <Name>.Create() ...")`.
- A `static <Name> Create(...)` factory that passes `<Name>Id.New`.
- Properties have **private setters**; expose behavior through methods (no public setters).
- Collect domain events with the `EntityBase` helper — they dispatch **after** a successful `SaveChanges` via `EventDispatchInterceptor`.
- Put query logic in `Domain/<Name>Aggregate/Specifications/` (e.g. `<Name>ByIdSpec`), not inline in handlers.

## 3. ⚠️ Register EVERY typed ID / value object in VogenEfCoreConverters

Edit `Infrastructure/Data/Config/VogenEfCoreConverters.cs` and add an attribute line — **EF cannot map the type without this, and the failure is a runtime mapping error, not a compile error**:

```csharp
[EfCoreConverter<ProductId>]
// ... existing ...
[EfCoreConverter<<Name>Id>]          // add this (and one line per extra value object)
internal partial class VogenEfCoreConverters;
```

## 4. EF entity configuration

Create `Infrastructure/Data/Config/<Name>Configuration.cs` (auto-applied via `ApplyConfigurationsFromAssembly`). Mirror `ProductConfiguration.cs`:

- For an **int** key, wire the value generator that turns the `From(0)` sentinel into a real key:
  ```csharp
  builder.Property(e => e.Id)
    .HasValueGenerator<VogenIntIdValueGenerator<AppDbContext, <Name>, <Name>Id>>()
    .HasVogenConversion()
    .IsRequired();
  ```
  For a **Guid** key use `VogenGuidIdValueGenerator<...>` instead.
- Configure the remaining properties (`HasMaxLength`, `HasPrecision`, `IsRequired`, relationships).
- Seed dev data with `builder.HasData(...)` using explicit `<Name>Id.From(n)` values if needed.

## 5. ⚠️ Extend the NsDepCop boundary rules

`config.nsdepcop` is compiled with `NSDEPCOP01` as an **error** — a wrong dependency direction fails the build. Add the analogous rules for the new namespaces:

```xml
<Disallowed From="Parkin.Api.Domain.<Name>Aggregate.*"
            To="Parkin.Api.Infrastructure.*" />
```

Add a `*Features.* → Infrastructure.*` disallow when you add the feature slice, and cross-aggregate disallows where aggregates must not reference each other (see the existing `Order → Cart` rule).

## 6. Feature slice (when adding endpoints)

Create `<Name>Features/<Op>/` colocating Endpoint + Request/Response + Validator + Mapper, and a Command/Query + Handler for write/cross-aggregate ops. Simple CRUD may use the repository directly in the endpoint; orchestration goes through `IMediator` (source-gen `Mediator`: handlers return `ValueTask<T>`, commands are `ICommand<Result<T>>`). Endpoints map `Ardalis.Result` to typed HTTP results.

## 7. Migration & build

1. `cd backend`
2. `dotnet ef migrations add Add<Name> --project src/Parkin.Api`  (see the `create-migration` skill)
3. `dotnet ef database update --project src/Parkin.Api`
4. `dotnet build` — remember `TreatWarningsAsErrors=true`; warnings fail the build.

## Sanity checklist

- [ ] `<Name>Id` (+ value objects) created with the correct `New`/validation shape
- [ ] Aggregate: `EntityBase` + `IAggregateRoot`, private ctor, guarded ctor, `Create()`, private setters
- [ ] ⚠️ `[EfCoreConverter<<Name>Id>]` added to `VogenEfCoreConverters.cs`
- [ ] `<Name>Configuration` with the matching `Vogen*IdValueGenerator` + `HasVogenConversion()`
- [ ] ⚠️ `config.nsdepcop` rules extended
- [ ] Migration added, `dotnet build` green
