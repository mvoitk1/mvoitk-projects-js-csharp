# Onion/Clean Architecture — Project Split Plan

## 0. Goal & target shape

Refactor the current 8-project solution into a strict onion layout that separates
**generic framework code (`Base.*`)** from **app-specific code (`App.*`)** and splits
each layer into **Contracts**, **DTO**, and **Implementation** projects so that:

- Higher layers depend on lower layers only through *interfaces* (Contracts), never
  through implementations.
- DAL-level data shapes (`App.DAL.DTO`) and BLL-level data shapes (`App.BLL.DTO`) are
  distinct from each other and from the public API surface (`App.DTO`).
- `WebApp` knows nothing about EF Core, `AppDbContext`, or repository internals — only
  about service contracts and public DTOs.

### Current (8 projects)
```
App.Domain, App.DAL.EF, App.BLL, App.DTO, App.Helpers,
App.Resources, Base.Resources, WebApp, WebApp.Tests
```

### Target (16 projects)
```
Base.Contracts            (generic IDomainEntityId, etc.)
Base.Domain               (generic DomainEntityId<TKey>)
Base.Helpers              (renamed from App.Helpers)
Base.BLL.Contracts        (generic IEntityService<TEntity, TDto, TKey>)
Base.BLL                  (generic BaseEntityService impl)
Base.DAL.Contracts        (generic IBaseRepository, IBaseUnitOfWork)
Base.DAL.EF               (generic BaseRepository, BaseUnitOfWork impls)

App.Domain                (entities — inherit Base.Domain)
App.DAL.Contracts         (IAppUnitOfWork, IProductRepository, ...)
App.DAL.DTO               (DAL-level data carriers, if any)
App.DAL.EF                (AppDbContext + repo + UoW impl)
App.BLL.Contracts         (IProductService, ICartService, ...)
App.BLL.DTO               (BLL-internal DTOs)
App.BLL                   (service implementations + mappers)
App.DTO                   (public API DTOs — already exists, stays)
App.Resources             (unchanged)
Base.Resources            (unchanged)

WebApp                    (controllers, MVC, DI wiring)
WebApp.Tests              (xUnit)
```

Net add: **8 new projects.** Net rename: **1** (`App.Helpers` → `Base.Helpers`).

---

## 1. Dependency graph (what references what)

Read top→bottom = "depends on". A project may only reference what is *below* it.

```
WebApp
 ├─► App.BLL.Contracts          (DI: register IFooService)
 ├─► App.DAL.Contracts          (DI: register IAppUnitOfWork)
 ├─► App.DAL.EF                 (DI wiring ONLY — register impls, AddDbContext)
 ├─► App.BLL                    (DI wiring ONLY — register impls)
 ├─► App.DTO                    (controllers return these)
 ├─► App.BLL.DTO                (mapped to App.DTO in controllers or thin mapper)
 └─► App.Resources              (.resx for views)

App.BLL  ──► App.BLL.Contracts, App.BLL.DTO,
             App.DAL.Contracts, App.DAL.DTO,
             Base.BLL, Base.Helpers, App.Domain *(only when entity creation is required)*

App.BLL.Contracts ──► App.BLL.DTO, Base.BLL.Contracts, Base.Contracts

App.BLL.DTO       ──► Base.Contracts        (so DTOs can implement IDomainEntityId)

App.DAL.EF        ──► App.DAL.Contracts, App.DAL.DTO, App.Domain,
                       Base.DAL.EF, Base.Helpers
                       (+ EF Core, Identity.EntityFrameworkCore, Npgsql packages)

App.DAL.Contracts ──► App.DAL.DTO, App.Domain, Base.DAL.Contracts, Base.Contracts

App.DAL.DTO       ──► Base.Contracts

App.Domain        ──► Base.Domain, Base.Contracts
                       (+ Identity.EntityFrameworkCore for AppUser/AppRole only)

Base.BLL          ──► Base.BLL.Contracts, Base.DAL.Contracts, Base.Contracts
Base.BLL.Contracts ─► Base.Contracts
Base.DAL.EF       ──► Base.DAL.Contracts, Base.Domain, Base.Contracts (+ EF Core)
Base.DAL.Contracts ─► Base.Domain, Base.Contracts
Base.Domain       ──► Base.Contracts
Base.Helpers      ──► (none)
Base.Contracts    ──► (none)
```

### What `WebApp` MUST NOT reference directly
- ❌ `AppDbContext` instances or any `DbSet<>` — go through `IAppUnitOfWork`.
- ❌ Repository classes (concrete) — only `IFooRepository` if absolutely necessary,
  preferably only `IAppUnitOfWork`.
- ❌ Service classes (concrete) — only `IFooService`.
- ❌ Entity types from `App.Domain` in controller signatures, action results, or
  bound view models. Domain entities live behind the DAL boundary.

The single concession: `WebApp/Program.cs` (or `Setup/`) imports
`App.DAL.EF` + `App.BLL` namespaces solely to call `services.AddDbContext<>()` and
`services.AddScoped<IFooService, FooService>()`. **Move this wiring into extension
methods** (`AddAppDataAccess(this IServiceCollection)` in `App.DAL.EF`,
`AddAppBusinessLogic(this IServiceCollection)` in `App.BLL`) so `WebApp` only
calls them — the implementation types stay encapsulated.

---

## 2. Contents of each new project

### `Base.Contracts/`
```csharp
public interface IDomainEntityId : IDomainEntityId<Guid> { }
public interface IDomainEntityId<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; set; }
}
```
Replaces `App.Domain/IBaseEntity.cs`. Generic over key type so future non-Guid
entities (if any) work.

### `Base.Domain/`
```csharp
public abstract class DomainEntityId : DomainEntityId<Guid>, IDomainEntityId { }
public abstract class DomainEntityId<TKey> : IDomainEntityId<TKey>
    where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
}
```
Replaces `App.Domain/BaseEntity.cs`. `App.Domain` entities then change from
`: BaseEntity` to `: DomainEntityId` (or `: DomainEntityId<Guid>`).

### `Base.Helpers/`
Move `App.Helpers/JsonHelpers.cs` here as-is. Rename csproj. Delete old project.
(Anything truly app-specific stays out — current `JsonHelpers` is generic so it
fits here.)

### `Base.DAL.Contracts/`
```csharp
public interface IBaseRepository<TEntity> : IBaseRepository<TEntity, Guid>
    where TEntity : class, IDomainEntityId { }

public interface IBaseRepository<TEntity, TKey>
    where TEntity : class, IDomainEntityId<TKey>
    where TKey : IEquatable<TKey>
{
    Task<IEnumerable<TEntity>> AllAsync();
    Task<TEntity?> FindAsync(TKey id);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}

public interface IBaseUnitOfWork
{
    Task<int> SaveChangesAsync();
}
```
The current `App.DAL.EF/Repositories/IBaseRepository.cs` collapses into this.

### `Base.DAL.EF/`
Generic `BaseRepository<TEntity, TDbContext>` and an optional
`BaseUnitOfWork<TDbContext>`. Body of current
`App.DAL.EF/Repositories/BaseRepository.cs` is lifted here and made generic over
`TDbContext : DbContext`. The `Set<TEntity>()` call already works generically.

Package refs: `Microsoft.EntityFrameworkCore`.

### `Base.BLL.Contracts/`
Optional but recommended: a generic `IEntityService<TDalDto, TBllDto, TKey>`
contract for CRUD use-cases. Skip if it doesn't fit any current service cleanly
(most current services have richer interfaces — keep them in
`App.BLL.Contracts`). The project still exists as the future home for cross-cutting
service contracts.

### `Base.BLL/`
Optional implementation of the generic service. Same rationale as above. Project
exists so the symmetry is preserved.

### `App.DAL.Contracts/`
Move from `App.DAL.EF/`:
- `Repositories/IBaseRepository.cs` — **deleted** (replaced by Base version).
- `Repositories/IProductRepository.cs`, `ICategoryRepository.cs`,
  `ICollectionRepository.cs`, `ICartRepository.cs`, `ICartItemRepository.cs`,
  `IOrderRepository.cs`, `IProductVariantRepository.cs`,
  `IProductImageRepository.cs`, `IProductCategoryRepository.cs`,
  `IColorRepository.cs`, `ISizeRepository.cs`.
- `UnitOfWork/IAppUnitOfWork.cs`.

Each interface changes its base from `IBaseRepository<TEntity>` (App-defined) to
`Base.DAL.Contracts.IBaseRepository<TEntity>`. Namespaces become
`App.DAL.Contracts.Repositories` / `App.DAL.Contracts.UnitOfWork`.

### `App.DAL.DTO/`
Today the repos return raw entities. Two paths:

- **Pragmatic (recommended for this assignment):** create the project but leave it
  empty initially — add DTOs only when a repo legitimately needs to project away
  from the entity (e.g. `OrderListItemDal` used by `GetUserOrders` to avoid loading
  the full graph). The project must exist so `App.DAL.Contracts` can return DAL
  DTOs without leaking entity types in the future.
- **Strict onion:** mirror every entity with a DAL DTO and have repos return DTOs
  exclusively. Costly and not required by the rubric.

Pick pragmatic. Note the rule: *if a repo returns a DAL DTO, the entity-to-DAL-DTO
mapper lives in `App.DAL.EF/Mappers/`*.

### `App.DAL.EF/`
After moves: keeps `AppDbContext.cs`, `Migrations/`, `Seeding/`, repository
**implementations**, `AppUnitOfWork` **implementation**, and (new) a `Setup/`
folder with `services.AddAppDataAccess(IConfiguration)` extension.

### `App.BLL.Contracts/`
Move from `App.BLL/Services/`:
- All `IXxxService.cs` files (`IProductService`, `IAdminProductService`,
  `ICartService`, `ICategoryService`, `ICollectionService`, `IOrderService`,
  `IAdminCatalogueService`, `IAdminOrderService`).

Service interfaces change their DTO references from `App.DTO.v1.*` to
`App.BLL.DTO.*` (see next).

### `App.BLL.DTO/`
This is the **internal data shape between BLL and the API layer**. Two viable
patterns:

- **Mirror App.DTO (recommended):** copy every DTO from `App.DTO/v1/` into
  `App.BLL.DTO/` (same shape, neutral names — drop the `v1` folder since
  versioning is an API concern). Services return `App.BLL.DTO` types; controllers
  map to `App.DTO.v1.*` for the wire. A small `App.DTO/Mappers/` (or inline
  one-liner expression in the controller) handles the BLL→API mapping.
- **Reuse App.DTO:** services keep returning `App.DTO.v1.*` and `App.BLL.DTO`
  remains empty. Cheaper but loses the rubric point for the layer.

Pick **Mirror**. The naming convention: `App.BLL.DTO.ProductDto`,
`App.BLL.DTO.Admin.AdminProductDto`, etc. Same folder structure (Admin, Cart,
Categories, Collections, Identity, Orders, Products).

### `App.BLL/`
After moves: keeps `Services/` (impl classes only), keeps `Mappers/` (now
mapping `Domain ↔ App.BLL.DTO` rather than `Domain ↔ App.DTO`), adds a `Setup/`
folder with `services.AddAppBusinessLogic()` extension.

### `App.DTO/`
**Unchanged structurally.** Stays the public API DTO surface
(`App.DTO/v1/...`). Add a thin `Mappers/` if BLL.DTO → API DTO is non-trivial;
otherwise inline `Select(x => new ProductDto { Id = x.Id, ... })` in the
controller is fine.

---

## 3. Move map (file-by-file)

| From                                                            | To                                              |
|-----------------------------------------------------------------|-------------------------------------------------|
| `App.Domain/IBaseEntity.cs`                                     | `Base.Contracts/IDomainEntityId.cs` *(rewrite)* |
| `App.Domain/BaseEntity.cs`                                      | `Base.Domain/DomainEntityId.cs` *(rewrite)*     |
| `App.Helpers/JsonHelpers.cs`                                    | `Base.Helpers/JsonHelpers.cs`                   |
| `App.DAL.EF/Repositories/IBaseRepository.cs`                    | `Base.DAL.Contracts/IBaseRepository.cs`         |
| `App.DAL.EF/Repositories/BaseRepository.cs`                     | `Base.DAL.EF/BaseRepository.cs` *(generic)*     |
| `App.DAL.EF/Repositories/I{Product,Category,...}Repository.cs`  | `App.DAL.Contracts/Repositories/...`            |
| `App.DAL.EF/Repositories/{Product,Category,...}Repository.cs`   | *(stay in App.DAL.EF)*                          |
| `App.DAL.EF/UnitOfWork/IAppUnitOfWork.cs`                       | `App.DAL.Contracts/UnitOfWork/IAppUnitOfWork.cs`|
| `App.DAL.EF/UnitOfWork/AppUnitOfWork.cs`                        | *(stay in App.DAL.EF)*                          |
| `App.BLL/Services/I*Service.cs`                                 | `App.BLL.Contracts/I*Service.cs`                |
| `App.BLL/Services/*Service.cs`                                  | *(stay in App.BLL)*                             |
| `App.BLL/Mappers/*Mapper.cs`                                    | *(stay in App.BLL — retarget to App.BLL.DTO)*   |
| *(new)* mirrored DTOs                                            | `App.BLL.DTO/{Admin,Cart,Categories,...}/...`   |
| `App.DTO/v1/...`                                                | *(unchanged — public API surface)*              |

**Entity edits required after `Base.Domain` exists:** every entity in
`App.Domain/` that inherits `BaseEntity` (currently 13 of them: Cart, CartItem,
Category, Collection, Color, Order, OrderItem, Product, ProductCategory,
ProductImage, ProductVariant, Size, plus the Identity ones that override IdentityUser
won't change) needs `: BaseEntity` → `: DomainEntityId`. Mechanical rename.

---

## 4. Base.* vs App.* — the discriminator

**Base.\*** contains code that has **no knowledge of this specific application's
domain**. If you started a new project tomorrow with completely different entities,
the entire `Base.*` family could be copy-pasted unchanged.

- `Base.Contracts/IDomainEntityId` — works for any entity with any key type.
- `Base.Domain/DomainEntityId` — works for any entity.
- `Base.DAL.Contracts/IBaseRepository<T>` — generic CRUD contract.
- `Base.DAL.EF/BaseRepository<T, TCtx>` — generic CRUD impl over any DbContext.
- `Base.Helpers/JsonHelpers` — utility, no domain ties.

**App.\*** contains code **specific to this fashion e-commerce platform**.

- `App.Domain/Product`, `Order`, `Cart`, … — actual entities.
- `App.DAL.Contracts/IProductRepository.GetListFiltered(...)` — encodes
  product-listing query semantics specific to this catalogue.
- `App.DAL.EF/AppDbContext` — references concrete entities.
- `App.BLL.Contracts/IOrderService.PlaceOrderAsync` — domain use-case.
- `App.BLL.DTO/ProductDto` — shape that carries this app's product data.

Test: "could `Base.Foo` go into a NuGet package and be consumed by an unrelated
solution?" If yes, it belongs in Base; otherwise App.

---

## 5. Execution order (build stays green at every step)

Each step is a single commit. After each, run `dotnet build` and `dotnet test`.
Order is chosen so that no project ever references a project that hasn't been
created and populated yet.

### Step 1 — Create empty Base.* and App.* contract/DTO projects

`dotnet new classlib` × 8, then `dotnet sln add` each one:

```bash
dotnet new classlib -n Base.Contracts        -f net10.0
dotnet new classlib -n Base.Domain           -f net10.0
dotnet new classlib -n Base.Helpers          -f net10.0
dotnet new classlib -n Base.DAL.Contracts    -f net10.0
dotnet new classlib -n Base.DAL.EF           -f net10.0
dotnet new classlib -n Base.BLL.Contracts    -f net10.0
dotnet new classlib -n Base.BLL              -f net10.0
dotnet new classlib -n App.DAL.Contracts     -f net10.0
dotnet new classlib -n App.DAL.DTO           -f net10.0
dotnet new classlib -n App.BLL.Contracts     -f net10.0
dotnet new classlib -n App.BLL.DTO           -f net10.0
dotnet sln webapp2025s.sln add Base.Contracts Base.Domain Base.Helpers \
    Base.DAL.Contracts Base.DAL.EF Base.BLL.Contracts Base.BLL \
    App.DAL.Contracts App.DAL.DTO App.BLL.Contracts App.BLL.DTO
```

Wire the package refs (EF Core for `Base.DAL.EF`, none for the rest yet) and the
project refs per the dependency graph in §1. **Don't add references from existing
projects yet.** Build — should still compile because nothing depends on the empties.

### Step 2 — Populate Base.Contracts, Base.Domain, Base.Helpers

Write `IDomainEntityId` (new file), `DomainEntityId` (new file), copy
`JsonHelpers.cs` into `Base.Helpers/`. **Don't remove the originals yet.**

Add to `App.Domain.csproj`:
```xml
<ProjectReference Include="..\Base.Domain\Base.Domain.csproj" />
<ProjectReference Include="..\Base.Contracts\Base.Contracts.csproj" />
```

In `App.Domain/`, mass-replace `: BaseEntity` with `: DomainEntityId` and
`IBaseEntity` with `IDomainEntityId`. Delete `App.Domain/BaseEntity.cs` and
`App.Domain/IBaseEntity.cs`. Update `using`s in `App.DAL.EF/Repositories/*` to
`using Base.Contracts;`.

Build. Run tests.

### Step 3 — Populate Base.DAL.Contracts + Base.DAL.EF (generic repo)

Write the generic `IBaseRepository<T, TKey>` + `IBaseRepository<T>` in
`Base.DAL.Contracts`. Write generic `BaseRepository<T, TCtx>` in `Base.DAL.EF`.

In `App.DAL.EF`:
- Add project refs to `Base.DAL.Contracts` and `Base.DAL.EF`.
- Delete `App.DAL.EF/Repositories/IBaseRepository.cs` and
  `App.DAL.EF/Repositories/BaseRepository.cs`.
- Change every concrete `XxxRepository : BaseRepository<XxxEntity>` to inherit
  from the generic `Base.DAL.EF.BaseRepository<XxxEntity, AppDbContext>`.
- Change every `IXxxRepository : IBaseRepository<XxxEntity>` to use the Base one.

Build. Run tests.

### Step 4 — Move repository interfaces to App.DAL.Contracts

`git mv` `App.DAL.EF/Repositories/I*.cs` → `App.DAL.Contracts/Repositories/`.
`git mv` `App.DAL.EF/UnitOfWork/IAppUnitOfWork.cs` →
`App.DAL.Contracts/UnitOfWork/`.

Update namespaces (`App.DAL.EF.Repositories` → `App.DAL.Contracts.Repositories`,
etc.). Add project refs:
- `App.DAL.Contracts` → `App.Domain`, `Base.DAL.Contracts`, `Base.Contracts`.
- `App.DAL.EF` → `App.DAL.Contracts` (concrete impls implement the moved
  interfaces).
- `App.BLL` → `App.DAL.Contracts` (so it can `using` the interfaces). Remove
  `App.BLL`'s direct reference to `App.DAL.EF`. *(Verify nothing in
  `App.BLL/Services/*` still touches `AppDbContext`. If it does, that's pre-existing
  technical debt — fix or defer.)*

Build. Run tests.

### Step 5 — Move service interfaces to App.BLL.Contracts

`git mv` `App.BLL/Services/I*.cs` → `App.BLL.Contracts/`. Update namespaces.
Project refs:
- `App.BLL.Contracts` → `App.BLL.DTO` *(refs the future DTO project — empty for
  now, will populate next step)*, `Base.BLL.Contracts`, `Base.Contracts`.
- `App.BLL` → `App.BLL.Contracts`.
- `WebApp` → `App.BLL.Contracts` (controllers use `IFooService`).

**At this point the service interfaces still reference `App.DTO.v1.*` types.**
Add a temporary `App.BLL.Contracts → App.DTO` reference so it compiles. This
reference will be removed in Step 6.

Build. Run tests.

### Step 6 — Populate App.BLL.DTO (mirror App.DTO) + retarget services and mappers

This is the biggest single step but it's mechanical:

1. Copy every file under `App.DTO/v1/` to `App.BLL.DTO/` with namespace
   `App.BLL.DTO.{Admin|Cart|...}`. Drop the `v1` segment from folder paths and
   namespaces — versioning is API-only.
2. In `App.BLL.Contracts/I*Service.cs` and `App.BLL/Services/*Service.cs` and
   `App.BLL/Mappers/*Mapper.cs`, replace `using App.DTO.v1.X;` with
   `using App.BLL.DTO.X;`.
3. Add `App.BLL.Contracts → App.BLL.DTO` ref (already added in Step 5).
4. Remove `App.BLL.Contracts → App.DTO` and `App.BLL → App.DTO` refs.
5. In `WebApp/ApiControllers/v1/*.cs`, every controller action that calls a
   service now gets back an `App.BLL.DTO.*`. Map it to `App.DTO.v1.*` before
   returning. For most DTOs this is a property-for-property copy — either inline
   (`new ProductDto { Id = bllDto.Id, … }`) or via a small mapper class in
   `App.DTO/Mappers/`. Pick mappers if there are ≥5 properties or any nested
   list; inline otherwise.

Build. Run tests. **Expect this commit to be large** — that's fine, it's a
contained translation.

### Step 7 — Promote App.Helpers → Base.Helpers

Already-created `Base.Helpers` is populated. Now:
- Switch every `using App.Helpers;` to `using Base.Helpers;`.
- Switch every `<ProjectReference Include="..\App.Helpers\...">` to point at
  `Base.Helpers`.
- `dotnet sln remove App.Helpers/App.Helpers.csproj`.
- `rm -rf App.Helpers/`.

Build. Run tests.

### Step 8 — Add DI extension methods, clean up WebApp refs

Create:
- `App.DAL.EF/Setup/AppDataAccessExtensions.cs` with
  `AddAppDataAccess(this IServiceCollection, IConfiguration)` that calls
  `AddDbContext<AppDbContext>(...)` and registers `IAppUnitOfWork` +
  `AppUnitOfWork`.
- `App.BLL/Setup/AppBusinessLogicExtensions.cs` with
  `AddAppBusinessLogic(this IServiceCollection)` that registers every
  `IFooService → FooService`.

In `WebApp/Program.cs` (or `WebApp/Setup/AppServicesExtensions.cs`):
```csharp
builder.Services.AddAppDataAccess(builder.Configuration);
builder.Services.AddAppBusinessLogic();
```

Audit `WebApp.csproj` references. Keep:
- `App.BLL.Contracts`, `App.DAL.Contracts`, `App.DTO`, `App.BLL.DTO`,
  `App.Resources`.
- `App.BLL`, `App.DAL.EF` — required so the extension methods are visible and the
  DI container can construct the impls. Document this as the single allowed
  exception.

Drop direct `WebApp → App.Domain` if present (controllers should never reference
domain entities). Some `Migrations` design-time tooling needs `App.DAL.EF` —
that's already covered by the kept ref.

Build. Run tests. Smoke-test the running app.

### Step 9 — Validate the boundary

Quick `grep` audit:

```bash
# WebApp must not use AppDbContext directly
grep -rn "AppDbContext" WebApp/ --include="*.cs" \
  | grep -v "Setup/" | grep -v "Program.cs"   # should be empty

# WebApp must not return domain entities
grep -rn "App.Domain" WebApp/ApiControllers/ --include="*.cs"   # should be empty

# App.BLL must not touch AppDbContext
grep -rn "AppDbContext" App.BLL/ --include="*.cs"               # should be empty

# App.BLL.Contracts must not reference App.DTO
grep -n "App.DTO" App.BLL.Contracts/*.csproj                    # should be empty
```

Any hit is a leak. Fix before considering the refactor done.

---

## 6. Done when

- All 16 projects exist and reference exactly what §1 says they reference (no
  more, no less).
- `dotnet build` is green.
- `dotnet test` is green (existing integration tests untouched in behaviour —
  only their `using` statements may have changed).
- The four grep audits in Step 9 return no results.
- `WebApp/Program.cs` registers infrastructure via `AddAppDataAccess()` /
  `AddAppBusinessLogic()` rather than naming concrete service or repo types.
- Domain entities never appear in API controller signatures or return types.

---

## 7. Out of scope (do not change as part of this refactor)

- Entity shapes / database schema / EF migrations — `AppDbContext.OnModelCreating`
  stays as-is; `Base.Domain` introduces no new persisted columns.
- LangStr / translations / .resx files — `App.Resources` and `Base.Resources`
  are untouched.
- JWT auth flow, Swagger config, localisation middleware.
- Seeding (`AppDataInit.cs`) — stays in `App.DAL.EF/Seeding/`.
- The repository, UoW, mapper, and test work covered by the prior `onion_plan.md`
  (now superseded). If any of that is still incomplete, finish it *before* the
  project split — the split mechanically relocates files but does not create new
  ones besides interfaces and DTO mirrors.
