# Modular Monolith — Refactor Plan (Assignment 5, Phase 3)

## 0. Goal

Convert the current onion-layered solution (Assignment 3, Phase 2) into a true **modular monolith**:

- **3 modules**, each a self-contained vertical slice (Domain + DAL + BLL + Web + Contracts).
- **No direct project references between modules.** Modules talk only through **MediatR** requests/notifications published over a shared `Modules.SharedKernel` contract assembly.
- One ASP.NET host (`WebApp`) acts as a thin **composition root** that registers every module via an `IModule.Register(IServiceCollection, IConfiguration)` extension.
- One physical Postgres database, **one EF schema per module**, each module owning its own `DbContext` + migrations. No cross-schema FKs.

Required deliverables (from the assignment) all stay in scope: REST v1 controllers, Swagger, JWT, MVC client UX, full admin Area UX with view models (no `ViewBag`/`ViewData`), `.resx` translations + `LangStr` DB translations, IDOR enforcement in REST controllers, repository/UoW/service/mapper layering **inside each module**, test coverage, CI/CD.

---

## 1. Module catalog

| Module    | Entities (already exist in [App.Domain/](../App.Domain/))                                                                                  | Owns                                                                |
|-----------|--------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------|
| **Users**   | `AppUser`, `AppRole`, `AppRefreshToken`, (new) `Address`                                                                                   | Identity, JWT issuance, refresh tokens, address book                |
| **Catalog** | `Category`, `Collection`, `Color`, `Size`, `Product`, `ProductCategory`, `ProductImage`, `ProductVariant`                                  | Product/variant model, pricing read-side, stock authoritative state |
| **Sales**   | `Cart`, `CartItem`, `Order`, `OrderItem`                                                                                                   | Cart lifecycle, checkout, orders                                    |

Entity count: 4 (Users incl. new `Address`) + 8 (Catalog) + 4 (Sales) = **16**, comfortably above the 10-entity floor and unchanged from [ERD.md](ERD.md) apart from `Address`.

Why these three:
- **Users** is mandated by the assignment.
- **Catalog** and **Sales** are the natural seam in this codebase — today's [CartService.cs](../App.BLL/Services/CartService.cs) and [OrderService.cs](../App.BLL/Services/OrderService.cs) reach into `ProductVariants`, which is exactly the cross-boundary call we need to convert to MediatR.

---

## 2. Target solution layout

```
src/
  Shared/
    Modules.SharedKernel              (Guid PKs, LangStr, BaseEntity, Result<>, shared value objects)
    Modules.Contracts                 (MediatR contracts: IRequest/INotification + DTOs crossing modules)
    Modules.Abstractions              (IModule interface, IModuleRegistrar)

  Modules/
    Users/
      Users.Domain                    (AppUser, AppRole, AppRefreshToken, Address)
      Users.Application               (services, MediatR handlers for Users.Contracts requests)
      Users.Infrastructure            (UsersDbContext, repositories, AppUnitOfWork, migrations, seeding)
      Users.Web                       (Area "Account", AccountController + AddressController, AdminUsersController)
      Users.IntegrationEvents         (optional: events Users emits, e.g. UserRegistered)

    Catalog/
      Catalog.Domain
      Catalog.Application
      Catalog.Infrastructure
      Catalog.Web                     (ShopController, Area "Admin/Catalog")

    Sales/
      Sales.Domain
      Sales.Application
      Sales.Infrastructure
      Sales.Web                       (CartController, CheckoutController, OrdersController, Area "Admin/Orders")

  Hosts/
    WebApp                            (Program.cs, shared layout, /api/v1 versioning, Swagger, JWT bearer wiring, i18n middleware)

  BuildingBlocks/   (renamed from today's Base.* family — generic, app-agnostic)
    Base.Contracts, Base.Domain, Base.DAL.Contracts, Base.DAL.EF,
    Base.BLL.Contracts, Base.BLL, Base.Helpers, Base.Resources

tests/
  Modules.Users.Tests
  Modules.Catalog.Tests
  Modules.Sales.Tests
  WebApp.Tests.Postgres               (end-to-end across modules)
```

Per module the **internal** shape stays onion: `*.Domain` → `*.Infrastructure` (DAL+EF+UoW+repos) ← `*.Application` (BLL services + mappers + MediatR handlers) ← `*.Web` (REST v1 + MVC). The repo/UoW/service/mapper rubric points are satisfied **inside each module**.

---

## 3. Dependency rules (the only hard rules that matter)

1. **No `Modules/X` project may reference `Modules/Y/*`** — neither in `csproj` nor by `using`.
2. The only project a module may reference from outside its own folder:
   - `Shared/Modules.SharedKernel`
   - `Shared/Modules.Contracts`
   - `Shared/Modules.Abstractions`
   - `BuildingBlocks/Base.*`
3. `Hosts/WebApp` may reference every `*.Web` project (to compose Razor + controllers) **and every `*.Infrastructure` project** (DI registration only — concrete impls stay encapsulated behind their `AddXxxModule()` extension). It may **not** reference `*.Application` or `*.Domain` directly.
4. Cross-module calls go **only** through `IMediator.Send(...)` / `Publish(...)` using contracts from `Modules.Contracts`.
5. Tests for a module may reference `Modules.Contracts` + the module under test + `BuildingBlocks/Base.*` — never another module.

Enforce rule 1 mechanically with a single NetArchTest assertion in `WebApp.Tests.Postgres`:

```csharp
Types.InAssemblies(allModuleAssemblies)
     .Should()
     .NotHaveDependencyOnAny("Catalog", "Sales", "Users")   // any cross-module ref → fail
     .Where(t => !t.Namespace.StartsWith(t.Assembly.GetName().Name));
```

---

## 4. Cross-module communication via MediatR

### `Modules.Contracts` is the only place cross-module DTOs and requests live.

Folder layout:

```
Modules.Contracts/
  Catalog/
    Queries/
      GetVariantPricingQuery.cs       : IRequest<VariantPricingDto?>
      GetVariantsForCheckoutQuery.cs  : IRequest<IReadOnlyList<VariantForCheckoutDto>>
    Commands/
      ReserveStockCommand.cs          : IRequest<ReserveStockResult>
      ReleaseStockCommand.cs          : IRequest<Unit>
    Dtos/
      VariantPricingDto.cs            (Id, Sku, Price, IsActive, AvailableQty)
      VariantForCheckoutDto.cs
  Users/
    Queries/
      GetUserSnapshotQuery.cs         : IRequest<UserSnapshotDto?>
    Events/
      UserRegisteredEvent.cs          : INotification
  Sales/
    Events/
      OrderPlacedEvent.cs             : INotification     (Catalog handler decrements stock, Users handler can log)
      OrderCancelledEvent.cs          : INotification
```

### Concrete flows (replacing today's cross-cutting service code)

**Add-to-cart** — today [CartService.AddItemAsync](../App.BLL/Services/CartService.cs#L25) loads `ProductVariant` directly. After the split:

1. `Sales.Web.CartController` calls `ISalesCartService.AddItem(userId, dto)`.
2. `Sales.Application.CartService` issues `mediator.Send(new GetVariantPricingQuery(dto.ProductVariantId))`.
3. `Catalog.Application.Handlers.GetVariantPricingHandler` returns price + available qty from `CatalogDbContext`.
4. `Sales.Application.CartService` validates stock against the returned DTO, then writes to `SalesDbContext.CartItems` via its own UoW.

Sales never sees a `ProductVariant` entity; it sees a `VariantPricingDto`.

**Place-order** — today [OrderService.PlaceOrderAsync](../App.BLL/Services/OrderService.cs#L12) reads variants from the cart navigation, then mutates `item.ProductVariant.StockQty -= …`. After the split:

1. `Sales.Application.OrderService` loads its own cart from `SalesDbContext`.
2. Issues `mediator.Send(new ReserveStockCommand(items))`. The Catalog handler atomically decrements stock for all lines or returns a failure with which SKU was short.
3. On success, Sales writes the `Order` + `OrderItem`s to `SalesDbContext` and publishes `OrderPlacedEvent` via `mediator.Publish(...)`.
4. On any failure after stock reservation, Sales publishes `OrderFailedEvent` and Catalog's handler runs `ReleaseStockCommand` semantics. (For Phase 3 scope — single-process, single-DB — we can lean on a single ambient transaction across both `DbContext`s using `System.Transactions.TransactionScope` with `Enlist=true` in the connection string. Document this as the chosen consistency model in [DOCUMENTATION.md](DOCUMENTATION.md).)

**User-registered** — when `Users.Application.AccountService.Register` completes, it publishes `UserRegisteredEvent`. Sales handles it to pre-create an empty `Cart`. This replaces the implicit "create cart on first access" code in [CartService.GetOrCreateCartAsync](../App.BLL/Services/CartService.cs#L11).

### MediatR registration

In each module's `Add{Module}Module()`:

```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<UsersModuleMarker>());
```

`WebApp` calls each module's `Add{Module}Module()`. MediatR auto-discovers all handlers in all loaded module assemblies.

---

## 5. Database strategy

- **One Postgres database, three schemas: `users`, `catalog`, `sales`.**
- Each module owns its own `DbContext` configured in `OnModelCreating` with `modelBuilder.HasDefaultSchema("catalog")` etc.
- Each module has its own `Migrations/` folder. Migration commands become:
  ```bash
  dotnet ef migrations --project Modules/Catalog/Catalog.Infrastructure --startup-project Hosts/WebApp add <Name> -- --module Catalog
  dotnet ef database   --project Modules/Catalog/Catalog.Infrastructure --startup-project Hosts/WebApp update                 -- --module Catalog
  ```
  Implement a `--module` arg in `Program.cs` that picks which `DbContext` design-time tooling sees (or use one `IDesignTimeDbContextFactory` per module, which is cleaner).
- **No cross-schema FKs.** Where Sales needs a Catalog ID (e.g. `CartItem.ProductVariantId`, `OrderItem.ProductVariantId`) or a Users ID (`Cart.AppUserId`, `Order.AppUserId`), store a plain `Guid` with **no navigation property** and no FK constraint. Integrity is enforced by the MediatR handlers, not the database.
- `LangStr` (jsonb) stays exactly as in [CLAUDE.md](../CLAUDE.md) — each module declares `[Column(TypeName = "jsonb")]` on its own translatable columns.
- Seeding splits per module: `Users.Infrastructure/Seeding/UsersDataInit.cs` (admin user + roles), `Catalog.Infrastructure/Seeding/CatalogDataInit.cs` (categories/products), `Sales.Infrastructure/Seeding/` (empty by default). `Program.cs` runs them in dependency order: Users → Catalog → Sales.

---

## 6. Identity placement and JWT

The hardest constraint. ASP.NET Identity wants one `DbContext`; we have three.

Decision: **Users module owns the only Identity-aware `DbContext`** (`UsersDbContext : IdentityDbContext<AppUser, AppRole, Guid>`). Catalog and Sales `DbContext`s are plain `DbContext`s with no Identity tables.

- JWT issuance: `Users.Application.IdentityService` (moved verbatim from today's [IdentityService.cs](../App.BLL/Services/IdentityService.cs)).
- JWT validation middleware: configured in `Hosts/WebApp/Program.cs` (cross-cutting, lives in the host).
- Other modules never see `AppUser`. They read `userId` from the `ClaimsPrincipal` (which is what today's controllers already do) and use it as an opaque `Guid`.
- For admin views that list users (e.g. an admin order list showing customer name), `Sales.Application` issues `mediator.Send(new GetUserSnapshotQuery(userId))` and gets back `UserSnapshotDto { Id, FirstName, LastName, Email }`.

**IDOR**: stays exactly where it is today — every Sales REST controller filters by the JWT's `userId` claim, never by a route/body-supplied ID. The fact that Sales can't even *see* `AppUser` reinforces this.

---

## 7. Web composition

`Hosts/WebApp` is intentionally tiny:

```
Hosts/WebApp/
  Program.cs                          (≈40 lines: config, modules, middleware)
  appsettings.json
  Setup/
    JwtSetup.cs, SwaggerSetup.cs, LocalizationSetup.cs, MvcSetup.cs
  wwwroot/                            (shared static assets, shared admin layout)
  Views/Shared/_Layout.cshtml         (shared layout used by every module's views)
  Areas/Admin/_ViewStart.cshtml       (shared admin chrome)
```

Each module's `*.Web` project ships its own controllers + Razor views (compiled as a Razor Class Library — `<SdkBundleProject Sdk="Microsoft.NET.Sdk.Razor">`). The host picks them up automatically via `AddApplicationPart(typeof(CatalogModuleMarker).Assembly)` from the module's registration extension.

REST API versioning (`/api/v1/...`) is configured once in `Hosts/WebApp` via the existing `AddAppApiVersioning()`, then every module's `v1/` API controllers participate.

Swagger discovers all controllers across loaded assemblies automatically.

Admin Area UX: each module contributes admin controllers under `Areas/Admin/{ModuleName}/`. Shared `_Layout.cshtml` lives in the host so the admin chrome is consistent. Every admin view uses a strongly-typed `*ViewModel` class from the module's `Web/ViewModels/` folder — no `ViewBag`/`ViewData`.

Translations:
- **UI** (`.resx`): each module ships its own `App.Resources` equivalent (`Users.Web/Resources/`, `Catalog.Web/Resources/`, `Sales.Web/Resources/`) for view strings. Shared strings stay in `BuildingBlocks/Base.Resources`. Cultures: `en` (default), `et`.
- **DB** (`LangStr`): unchanged jsonb columns on entities in their respective modules.

---

## 8. Per-module project shape (Catalog as the worked example)

```
Modules/Catalog/
  Catalog.Domain/
    Entities/         (Product.cs, Category.cs, Collection.cs, Color.cs, Size.cs,
                       ProductImage.cs, ProductVariant.cs, ProductCategory.cs)
    Enums/Gender.cs

  Catalog.Application/
    Contracts/        (ICatalogProductService, ICatalogStockService — internal to module)
    Services/         (CatalogProductService, CatalogStockService — implementations)
    Mappers/          (ProductMapper: Product ↔ Catalog.Application.Dtos.ProductDto)
    Dtos/             (BLL-internal DTOs — not on the wire, not crossing modules)
    Handlers/         (MediatR handlers for Modules.Contracts.Catalog.* requests/events)
    DependencyInjection.cs   (AddCatalogApplication())

  Catalog.Infrastructure/
    CatalogDbContext.cs
    Migrations/
    Repositories/     (IProductRepository, ProductRepository, … — interfaces live here per
                       module, not in a separate Contracts project, to keep project count sane)
    UnitOfWork/       (ICatalogUnitOfWork, CatalogUnitOfWork)
    Seeding/CatalogDataInit.cs
    DependencyInjection.cs   (AddCatalogInfrastructure(IConfiguration))

  Catalog.Web/        (Razor Class Library)
    ApiControllers/v1/    (ProductsController, CategoriesController, CollectionsController)
    Areas/Admin/Catalog/Controllers/   (AdminProductsController, AdminCategoriesController, …)
    Areas/Admin/Catalog/Views/         (Razor views with strongly-typed view models)
    ViewModels/
    Resources/        (.resx for this module's UI strings)
    DependencyInjection.cs   (AddCatalogWeb() — ApplicationPart hookup)

  Catalog.Module.cs   (single file in module root: public static class implementing
                       AddCatalogModule(this IServiceCollection, IConfiguration) which calls
                       AddCatalogInfrastructure + AddCatalogApplication + AddCatalogWeb)
```

Users and Sales follow the identical shape with their own entity sets. **Five projects per module × 3 modules = 15 module projects**, plus 3 shared, plus 8 BuildingBlocks, plus 1 host, plus 4 test projects = **~31 projects**. That's a lot, but each is small, focused, and the dependency boundaries are crisp.

If 31 projects feels excessive for an assignment, the acceptable shortcut is to **merge `*.Application` and `*.Infrastructure` per module** (one combined `*.BackEnd` project), bringing the total to ~22. The MediatR contract boundary still works. Keep `*.Domain` and `*.Web` separate either way — those are the meaningful seams.

---

## 9. Migration order (keep `dotnet build` green at every step)

Each step = one commit; run `dotnet build` and `dotnet test` after each.

### Step 0 — fork
```bash
cp -R Csharp/modular_monolith Csharp/modular_monolith_a5
cd Csharp/modular_monolith_a5
git init && git add -A && git commit -m "phase 2 → phase 3 starting point"
```
Create a fresh GitLab repo and point `origin` at it (assignment requirement).

### Step 1 — introduce shared scaffolding
Add three new projects with no dependents yet:
- `Shared/Modules.SharedKernel` (move `BaseEntity`, `IBaseEntity`, `LangStr` here from `App.Domain`)
- `Shared/Modules.Abstractions` (the `IModule` interface, `AddModule<T>()` helper)
- `Shared/Modules.Contracts` (empty initially)

Add `MediatR` package to `Modules.Contracts` and `Hosts/WebApp` (host scans assemblies).

Build green. No behaviour change yet.

### Step 2 — carve out the Users module
This goes first because Identity is the most entangled.

1. Create `Modules/Users/{Users.Domain, Users.Application, Users.Infrastructure, Users.Web}`.
2. Move `App.Domain/Identity/*` → `Users.Domain/`.
3. Create `UsersDbContext : IdentityDbContext<…>` with **only** the Users tables. Copy the relevant `OnModelCreating` blocks from [AppDbContext.cs](../App.DAL.EF/AppDbContext.cs).
4. Move `App.BLL/Services/IdentityService.cs` (+ its `Mappers/IdentityMapper.cs`) → `Users.Application/`.
5. Move `App.DAL.EF/Repositories/RefreshTokenRepository.cs` → `Users.Infrastructure/Repositories/`.
6. Move `WebApp/ApiControllers/Identity/AccountController.cs` → `Users.Web/ApiControllers/v1/`.
7. Move the existing identity `.resx` files to `Users.Web/Resources/`.
8. In `Hosts/WebApp/Program.cs`, replace direct registrations with `services.AddUsersModule(builder.Configuration)`.
9. Generate the **initial** `users` schema migration; drop the Identity tables from the old `AppDbContext` (leave a manual SQL down-migration if you want to keep the existing DB).

Build + test green. The app still serves shop pages — they just now hit the Users module via DI.

### Step 3 — carve out the Catalog module
Analogous to Step 2. Moves:
- `App.Domain/{Product, ProductVariant, ProductImage, Category, Collection, Color, Size, ProductCategory}.cs` → `Catalog.Domain/`.
- All catalog/collection/category/color/size/productimage/productvariant repos → `Catalog.Infrastructure/Repositories/`.
- `App.BLL/Services/{ProductService, CategoryService, CollectionService, AdminProductService, AdminCatalogueService}.cs` → `Catalog.Application/`.
- Their interfaces + mappers + DTOs.
- `WebApp/ApiControllers/v1/{ProductsController, CategoriesController, …}` → `Catalog.Web/ApiControllers/v1/`.
- `WebApp/Controllers/ShopController.cs` → `Catalog.Web/Controllers/`.
- `WebApp/Areas/Admin/Controllers/{Products, Categories, Collections, Colors, Sizes}*` → `Catalog.Web/Areas/Admin/Catalog/Controllers/`.
- Views and resx accordingly.

Generate the `catalog` schema migration.

At this point `Sales` code in the old `App.BLL` will fail to compile because `CartService`/`OrderService` reference `ProductVariant`. **Temporarily** add a `Sales.Application → Catalog.Domain` direct reference to keep the build green. We'll remove it in Step 5.

Build + test green (with the temporary leak).

### Step 4 — carve out the Sales module
- `App.Domain/{Cart, CartItem, Order, OrderItem}.cs` → `Sales.Domain/`.
- `App.BLL/Services/{CartService, OrderService, AdminOrderService}.cs` → `Sales.Application/`.
- Their interfaces + mappers + DTOs.
- `WebApp/Controllers/{CartController, CheckoutController, OrdersController}.cs` → `Sales.Web/Controllers/`.
- `WebApp/Areas/Admin/Controllers/AdminOrdersController.cs` → `Sales.Web/Areas/Admin/Orders/Controllers/`.
- `WebApp/ApiControllers/v1/{CartController, OrdersController}` → `Sales.Web/ApiControllers/v1/`.

Generate the `sales` schema migration. Drop `App.DAL.EF`, `App.BLL`, `App.BLL.Contracts`, `App.BLL.DTO`, `App.DTO`, `App.DAL.Contracts`, `App.DAL.DTO`, `App.Domain`, `App.Resources` from the solution — every file should now live in a module.

Build green (still has the Step 3 temporary leak).

### Step 5 — replace the cross-module leak with MediatR

This is the architecturally important commit.

1. In `Modules.Contracts/Catalog/`, define `GetVariantPricingQuery`, `ReserveStockCommand`, `ReleaseStockCommand`, their DTOs and result types as listed in §4.
2. In `Catalog.Application/Handlers/`, implement the three handlers against `ICatalogUnitOfWork`. `ReserveStockCommand` must use a `SELECT … FOR UPDATE` (or EF `ExecuteUpdate` with a `WHERE StockQty >= @qty` predicate) to prevent oversell under concurrency.
3. In `Sales.Application/CartService`, replace `uow.ProductVariants.FindActiveAsync(...)` with `await mediator.Send(new GetVariantPricingQuery(dto.ProductVariantId))`.
4. In `Sales.Application/OrderService`, replace the in-place `item.ProductVariant.StockQty -= item.Quantity` block with `await mediator.Send(new ReserveStockCommand(...))` and handle the failure path.
5. After tests pass, delete the temporary `Sales.Application → Catalog.Domain` reference. **From here, no module references any other module — only `Modules.Contracts`.**
6. Add the NetArchTest assertion from §3 to `WebApp.Tests.Postgres` so this stays true forever.

Build + test green. Architecture is now correct.

### Step 6 — pull cart-on-register into an event

1. In `Modules.Contracts/Users/Events/`, define `UserRegisteredEvent : INotification`.
2. `Users.Application.IdentityService.Register` publishes the event after the user is committed.
3. `Sales.Application/Handlers/UserRegisteredHandler` creates an empty `Cart` row for the new user.
4. Strip the lazy "create on first access" branches from `CartService.GetOrCreateCartAsync` / `AddItemAsync` — they're now dead code for newly-registered users (keep the branch for users created before the migration, or backfill with a one-off migration that inserts an empty cart per user).

### Step 7 — split tests

1. Migrate the existing [WebApp.Tests/](../WebApp.Tests/) cases into the right per-module test project (most catalog tests → `Modules.Catalog.Tests`, most checkout tests → `Modules.Sales.Tests`, identity tests → `Modules.Users.Tests`).
2. Each module test project uses an in-memory or testcontainers Postgres pointed at *that module's schema only* — no cross-module setup required.
3. Keep `WebApp.Tests.Postgres` as the **integration** suite: spins up the full host, runs end-to-end through MediatR, covers the cross-module flows (add-to-cart → place-order). Reuses the testcontainers pattern from the existing project.
4. Add the NetArchTest "no cross-module reference" assertion here.
5. Coverage target: ≥70% line coverage per module's `*.Application` project. Wire `dotnet test --collect:"XPlat Code Coverage"` into CI.

### Step 8 — CI/CD

1. Rename the existing `deploy_csharp_a3` job in [.gitlab-ci.yml](../../.gitlab-ci.yml) to `deploy_csharp_a5` and point it at the new repo + a new container tag (`a5`).
2. New port on the VPS (`192.168.181.91`) to avoid colliding with the running A3 deploy — propose `83`. Update Nginx with a new server block.
3. Add a build stage that runs migrations against the deployed Postgres in dependency order: `users`, `catalog`, `sales`. Use the `--module` switch added in §5.
4. Keep secrets in GitLab CI variables exactly as today.
5. Healthcheck endpoint (already in [WebApp/](../WebApp/)) stays at `/healthz` and now verifies all three `DbContext`s can connect.

---

## 10. Done-when checklist

- [ ] 3 modules exist under `src/Modules/`. Each has its own Domain + Infrastructure + Application + Web project.
- [ ] `Hosts/WebApp/Program.cs` registers modules only by calling `services.AddUsersModule(...)`, `AddCatalogModule(...)`, `AddSalesModule(...)`. No concrete service or repository type names appear in `Program.cs`.
- [ ] `grep -r "Modules.Catalog" Modules/Sales/ Modules/Users/` → empty. Same for the other two permutations.
- [ ] Every cross-module call in any `*Service.cs` goes through `IMediator`. No `ProductVariant`, `AppUser`, or `Cart` type appears in a module that doesn't own it.
- [ ] NetArchTest suite passes in CI.
- [ ] Three EF schemas (`users`, `catalog`, `sales`) exist in the deployed database. No FK crosses schemas.
- [ ] REST controllers under `/api/v1/...` exist for products, cart, orders, account; Swagger lists them all under one document.
- [ ] JWT login + refresh works end-to-end.
- [ ] Admin Area UX is functional, protected by `[Authorize(Roles = "Admin")]`, and every view binds a strongly-typed `*ViewModel` (no `ViewBag`/`ViewData` references in any admin view).
- [ ] `.resx` files exist per module under `*.Web/Resources/`; `en` and `et` switch works in the browser.
- [ ] `LangStr`-typed `jsonb` columns return localized values in both API and MVC responses.
- [ ] IDOR test in each Sales REST controller: user A cannot read user B's cart or order by ID. Covered by integration tests.
- [ ] `dotnet test` ≥70% line coverage per `*.Application` project; coverage uploaded as a CI artifact.
- [ ] GitLab CI deploys the app + runs all three modules' migrations against the VPS Postgres; new app responds on `192.168.181.91:83`.
- [ ] [DOCUMENTATION.md](DOCUMENTATION.md) updated with the module map, the MediatR contract list, and the chosen consistency model (TransactionScope vs. compensating commands).

---

## 11. Risks and trade-offs

- **TransactionScope across two `DbContext`s** works on Npgsql but requires `Enlist=true` and Postgres ≥10. If you don't want distributed-ish transactions, switch to compensating commands (`ReleaseStockCommand` after a failed order write). The assignment doesn't grade this — pick one and document it.
- **Project count.** 22–31 projects is heavy; build time will roughly double vs. Phase 2. Acceptable for an academic project; for production you'd use Visual Studio solution filters.
- **Identity is the perennial exception.** Don't try to split `AppUser` across modules — it stays in `Users`. Other modules referencing users by `Guid` is the right pattern.
- **MediatR-only communication tempts overuse.** Avoid putting domain logic in handlers — they're thin adapters that delegate to a module's existing service. Otherwise the module's BLL becomes inverted and unreadable.
- **No cross-schema FKs means stock-vs-cart drift is possible** if you delete a `ProductVariant` while it sits in someone's cart. Document the contract: Catalog soft-deletes variants (set `IsActive = false`), never hard-deletes. Sales' `GetVariantPricingQuery` already returns `IsActive` — the cart UI can reject inactive variants on next view.

---

## 12. Out of scope

- Splitting into separate processes / microservices. The whole point is monolith deployment with module boundaries.
- Replacing MediatR with a message broker. In-process MediatR is exactly the right tool for a modular monolith.
- Touching the existing Phase 2 repo. This refactor lives in a fresh repo per the assignment.
- Adding payment processing, reviews, wishlist, email — still deferred per [DOCUMENTATION.md](DOCUMENTATION.md).
