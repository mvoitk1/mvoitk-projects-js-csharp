# Modular Monolith — Continuation Plan

Status as of last commit (`5 modules wired into Program.cs + per-module migrations`):

- **Architecture done.** 3 modules (Users, Catalog, Sales) — each with Domain / Application / Infrastructure / Web / Module projects. Zero cross-module project references. MediatR contracts + handlers in place. All three DbContexts on their own Postgres schemas (`users`, `catalog`, `sales`).
- **Request path still hits legacy.** API controllers in `WebApp/ApiControllers/v1/` and MVC controllers in `WebApp/Controllers/` + `WebApp/Areas/Admin/` are unchanged — they still inject `App.BLL.Contracts.I*Service` from the old DI graph. The new Catalog/Sales modules are loaded but unused at runtime.

## What's left, in execution order

### Phase A — Migrate Catalog services + DTOs into `Catalog.Application` (~25 files)

Move these files from old projects into `Catalog.Application`, renaming namespaces (`App.BLL` → `Catalog.Application`, `App.BLL.DTO` → `Catalog.Application.Dtos`, `App.Domain` → `Catalog.Domain`):

**Services + interfaces** (from [App.BLL/Services/](../App.BLL/Services/) + [App.BLL.Contracts/](../App.BLL.Contracts/)):
- `ProductService` + `IProductService`
- `CategoryService` + `ICategoryService`
- `CollectionService` + `ICollectionService`
- `AdminProductService` + `IAdminProductService`
- `AdminCatalogueService` + `IAdminCatalogueService`

**Mappers** (from [App.BLL/Mappers/](../App.BLL/Mappers/)):
- `ProductMapper`, `ProductVariantMapper`, `ProductImageMapper`
- `CategoryMapper`, `CollectionMapper`
- `AdminProductMapper`
- `MapperHelpers` (shared between modules — copy into both, or keep in `Modules.SharedKernel`)

**DTOs** (from [App.BLL.DTO/](../App.BLL.DTO/) `Products/`, `Categories/`, `Collections/`, `Admin/`):
- ~25 DTO files. Most are simple property bags — bulk `cp` + `sed 's/App\.BLL\.DTO/Catalog.Application.Dtos/g'` works.

After: register the 5 services in `CatalogApplicationServiceCollectionExtensions.AddCatalogApplication()`.

### Phase B — Migrate Sales remaining pieces (~8 files)

- `AdminOrderService` + `IAdminOrderService` (the customer name lookup uses `GetUserSnapshotQuery` via MediatR now — replace `e.AppUser?.FirstName` calls)
- `AdminOrderDto` (move from `App.BLL.DTO/Admin/`)
- `AdminOrderMapper`
- Register `IAdminOrderService` in `SalesApplicationServiceCollectionExtensions`.

### Phase C — Migrate REST controllers into `Catalog.Web` / `Sales.Web` (~10 files + DTOs)

Each `*.Web` already discovers its own controllers via `AddApplicationPart` in its `AddXxxWeb()` extension. So **moving a controller into the module's `ApiControllers/v1/` folder automatically registers it**.

**Catalog.Web/ApiControllers/v1/**:
- `ProductsController` (from `WebApp/ApiControllers/v1/`)
- `CategoriesController`
- `CollectionsController`

**Sales.Web/ApiControllers/v1/**:
- `CartController`
- `OrdersController`

For each: update the `using` imports to point at the new namespaces, and copy the matching public-API DTOs from `App.DTO/v1/` into `*.Web/Dtos/v1/` (these are the wire-format DTOs the controller returns — keep them separate from the internal `Application.Dtos`).

After moving each controller: delete the original from `WebApp/ApiControllers/v1/`.

### Phase D — Migrate MVC controllers + Razor views

This is the biggest single chunk because of the view files.

**Catalog.Web** picks up:
- `WebApp/Controllers/ShopController.cs` → `Catalog.Web/Controllers/`
- `WebApp/Areas/Admin/Controllers/{Products,Categories,Collections,Colors,Sizes}Controller.cs` → `Catalog.Web/Areas/Admin/Catalog/Controllers/`
- All matching view files under `WebApp/Views/Shop/` and `WebApp/Areas/Admin/Views/{Products,Categories,...}/`
- ViewModels from `WebApp/ViewModels/` (catalog-specific ones)

**Sales.Web** picks up:
- `WebApp/Controllers/{CartController,CheckoutController,OrdersController}.cs`
- `WebApp/Areas/Admin/Controllers/AdminOrdersController.cs`
- Matching views

**Razor Class Library quirks**: Views in an RCL are discovered automatically when the assembly has an `AddApplicationPart`. Verify `_ViewImports.cshtml` is copied per module so tag helpers and shared imports work.

### Phase E — Strip legacy projects

Once Phases A-D land and all controllers point at the new module services:

1. Remove from solution: `App.BLL`, `App.BLL.Contracts`, `App.BLL.DTO`, `App.DAL.EF`, `App.DAL.Contracts`, `App.DAL.DTO`, `App.Domain`, `App.DTO`, `App.Resources`.
2. Delete the directories.
3. Update `WebApp.csproj` to drop those `ProjectReference`s.
4. Update `WebApp.Tests.csproj` similarly; retarget any remaining `App.*` `using`s in tests to `Catalog.*`, `Sales.*`, `Users.*` namespaces.
5. Drop the legacy `AppDbContext` migration set ([App.DAL.EF/Migrations/](../App.DAL.EF/Migrations/)) — it duplicates what Catalog + Sales now own.
6. Remove `AppDbContext`-related code from [WebApp/Setup/AppDataInitExtensions.cs](../WebApp/Setup/AppDataInitExtensions.cs) — only the three module contexts remain.

### Phase F — Tests

- Split [WebApp.Tests/](../WebApp.Tests/) into per-module test projects (`Modules.Catalog.Tests`, `Modules.Sales.Tests`, `Modules.Users.Tests`). The existing integration tests in [WebApp.Tests/Integration/Api/](../WebApp.Tests/Integration/Api/) mostly fit cleanly: identity tests → Users, product tests → Catalog, cart/order tests → Sales.
- Keep [WebApp.Tests.Postgres](../WebApp.Tests.Postgres/) as the cross-module end-to-end suite (spins up the full host).
- Add the NetArchTest "no cross-module project reference" assertion from `modular_plan.md` §3.

### Phase G — CI/CD

- Rename `deploy_csharp_a3` → `deploy_csharp_a5` in [/Csharp/../.gitlab-ci.yml](../../.gitlab-ci.yml). Bump the VPS port from `82` to (proposed) `83` to avoid colliding with the running A3 deploy. Add Nginx server block.
- Migrations run order: `Users` → `Catalog` → `Sales`.

## Done-when checklist

- [ ] No file under `App.BLL/`, `App.BLL.Contracts/`, `App.BLL.DTO/`, `App.DAL.*`, `App.Domain/`, `App.DTO/` (delete the directories).
- [ ] `grep -rn "App.Domain\|App.BLL\|App.DAL" Csharp/modular_monolith/{Catalog,Sales,Users,WebApp,Modules}.* --include="*.cs"` returns empty.
- [ ] `grep -rn "Modules.Sales" Modules/Catalog/` and similar permutations return empty.
- [ ] Three migration sets exist (`Users.Infrastructure/Migrations`, `Catalog.Infrastructure/Migrations`, `Sales.Infrastructure/Migrations`); `App.DAL.EF/Migrations` is gone.
- [ ] `dotnet build` green, `dotnet test` green.
- [ ] Swagger lists APIs from all three `*.Web` modules under `/api/v1/`.
- [ ] Admin area resolves under all three modules; views render with strongly-typed view models.

## Estimated remaining work

- Phase A: ~40 file writes
- Phase B: ~8 file writes
- Phase C: ~15 file writes + DTO copies
- Phase D: ~50 file writes (views are bulk; controllers individually)
- Phase E: ~10 edits + 8 directory deletes
- Phase F: ~20 file writes
- Phase G: ~5 edits

Total: ~150 file operations + a handful of build/test cycles.

## Tips for the next session

- **Don't re-read every file you move.** `cat ... | sed` from old → new is faster for namespace-only changes.
- **Build per module, not the solution, while iterating.** `dotnet build Catalog.Module/Catalog.Module.csproj` is ~3× faster than the full sln.
- **Move one controller end-to-end first** as a smoke test of the ApplicationPart wiring before bulk-migrating others.
- **Migrations regen at the end.** Don't generate new migrations until the entire entity surface for that context is stable.
