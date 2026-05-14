# Clean/Onion Architecture — Gap Elimination Plan

Three rubric-mandatory gaps remain. This plan removes them without rewriting working
behaviour. Current state: BLL services inject `AppDbContext` directly and build DTOs
inline; there is no repository/UoW layer and no mapper classes.

Affected services (all use primary-constructor `AppDbContext db`):
`ProductService`, `CategoryService`, `CollectionService`, `CartService`,
`OrderService`, `AdminProductService`, `AdminOrderService`, `AdminCatalogueService`.

---

## Gap 1 — Repositories + Unit of Work

### Goal
BLL talks to `IAppUnitOfWork`, never to `AppDbContext`. Each entity has a repository
exposing query/command methods. `SaveChangesAsync` is called once per use-case via UoW.

### New files in `App.DAL.EF/`

```
App.DAL.EF/
  Repositories/
    IBaseRepository.cs        — generic contract
    BaseRepository.cs         — generic EF implementation (DbSet<TEntity>)
    IProductRepository.cs     — + GetListFiltered, GetWithVariantsAndImages
    ProductRepository.cs
    ICategoryRepository.cs
    CategoryRepository.cs
    ICollectionRepository.cs
    CollectionRepository.cs
    ICartRepository.cs        — GetActiveCartForUser (Include items + variant)
    CartRepository.cs
    IOrderRepository.cs       — GetUserOrders, GetUserOrderById (IDOR filter stays here)
    OrderRepository.cs
    IProductVariantRepository.cs / ProductVariantRepository.cs
    IColorRepository.cs / ColorRepository.cs
    ISizeRepository.cs / SizeRepository.cs
  UnitOfWork/
    IAppUnitOfWork.cs
    AppUnitOfWork.cs
```

### `IBaseRepository<TEntity>` surface
```
Task<IEnumerable<TEntity>> AllAsync();
Task<TEntity?> FindAsync(Guid id);
void Add(TEntity entity);
void Update(TEntity entity);
void Remove(TEntity entity);
```
`BaseRepository<TEntity>` holds a protected `DbSet<TEntity>` and `DbContext`.
Entity-specific repos inherit it and add Include-heavy query methods (the LINQ
currently inline in services moves here).

### `IAppUnitOfWork` surface
```
IProductRepository Products { get; }
ICategoryRepository Categories { get; }
ICollectionRepository Collections { get; }
ICartRepository Carts { get; }
IOrderRepository Orders { get; }
IProductVariantRepository ProductVariants { get; }
IColorRepository Colors { get; }
ISizeRepository Sizes { get; }
Task<int> SaveChangesAsync();
```
`AppUnitOfWork` takes `AppDbContext` in its constructor and lazily instantiates each
repository.

### DI registration
In `WebApp/Setup/AppServicesExtensions.cs` (or a new `AddAppRepositories()`):
```
services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();
```
Repositories do not need separate registration if only reached through UoW.

### Service migration (one service at a time, keep tests green between each)
1. Change constructor: `ProductService(IAppUnitOfWork uow)` instead of `AppDbContext db`.
2. Replace `db.Products.Include(...).Where(...)` with `uow.Products.GetListFiltered(...)`.
3. Replace `db.SaveChangesAsync()` with `uow.SaveChangesAsync()`.
4. Order of migration: `ProductService` → `CategoryService` → `CollectionService` →
   `CartService` → `OrderService` → `AdminCatalogueService` → `AdminProductService` →
   `AdminOrderService`.
5. **IDOR note:** `OrderService.GetUserOrderByIdAsync` filters by `AppUserId`. That
   filter moves into `OrderRepository.GetUserOrderById(userId, orderId)` — the
   user-scoping must stay enforced at the repository, not be optional.

### Done when
- No `using App.DAL.EF;` + `AppDbContext` injection remains in `App.BLL/Services/`.
- `App.BLL.csproj` still references `App.DAL.EF` (for UoW interface) — acceptable, or
  move `IAppUnitOfWork`/`IBaseRepository` to a new `App.Contracts` project for stricter
  onion (optional, not required to pass).
- Solution builds, existing integration tests pass unchanged.

---

## Gap 2 — Mappers

### Goal
Entity ↔ DTO translation lives in dedicated mapper classes, not inline in services.
The big `new ProductDto { ... }` / `new OrderDto { ... }` blocks move out.

### Approach: plain static mapper classes (no AutoMapper dependency)
Keeps it explicit, debuggable, and matches the existing manual style — just relocated.

### New files in `App.BLL/Mappers/`
```
App.BLL/Mappers/
  ProductMapper.cs      — Product -> ProductDto, Product -> ProductListItemDto
  ProductVariantMapper.cs
  ProductImageMapper.cs
  CategoryMapper.cs     — Category <-> CategoryDto / AdminCategoryDto / WriteDto
  CollectionMapper.cs
  CartMapper.cs         — Cart -> CartDto, CartItem -> CartItemDto
  OrderMapper.cs        — Order -> OrderDto / OrderListItemDto, OrderItem -> OrderItemDto
  AdminProductMapper.cs — Product <-> AdminProductDto / AdminProductWriteDto, variants
  AdminOrderMapper.cs
```

### Pattern
```csharp
public static class ProductMapper
{
    public static ProductDto ToDto(Product e) => new()
    {
        Id = e.Id,
        Name = e.Name.Translate() ?? string.Empty,
        // ... (lift the block currently in ProductService.GetByIdAsync)
        Variants = e.Variants?.Where(v => v.IsActive)
            .Select(ProductVariantMapper.ToDto).ToList() ?? [],
    };

    public static ProductListItemDto ToListItem(Product e) => new() { ... };
}
```
Write-DTO → entity mappers (for Admin create/update) take the existing entity when
updating so EF change-tracking still works:
```csharp
public static void ApplyWrite(AdminProductWriteDto dto, Product entity) { ... }
```

### Service changes
Replace inline `.Select(p => new ProductListItemDto { ... })` with
`.Select(ProductMapper.ToListItem)`; replace `new ProductDto { ... }` with
`ProductMapper.ToDto(product)`. `LangStr.Translate()` calls stay inside mappers.

### Done when
- No `new XxxDto { ... }` object initializers remain in `App.BLL/Services/`.
- Each service method body is use-case logic + repository calls + mapper calls only.

---

## Gap 3 — Test coverage

### Current state
`WebApp.Tests` has integration tests: IDOR (`IntegrationTestIDAR`), identity,
products, home controller; one unit test for HomeController. BLL services and mappers
are untested.

### Add — BLL unit tests (`WebApp.Tests/Unit/Services/`)
Use EF Core InMemory or SQLite-in-memory provider to back a real `AppUnitOfWork`.
Prefer SQLite in-memory (closer to relational behaviour; InMemory ignores constraints).

```
Unit/Services/
  ProductServiceTests.cs
    - GetListAsync filters by category / collection / gender
    - GetListAsync excludes inactive products
    - GetByIdAsync returns null for inactive or missing id
    - GetByIdAsync maps variants/images/categories correctly
  OrderServiceTests.cs
    - PlaceOrderAsync throws when no active cart
    - PlaceOrderAsync throws on insufficient stock
    - PlaceOrderAsync deducts stock, clears cart, sets CheckedOut
    - GetUserOrderByIdAsync returns null for another user's order  (IDOR at BLL level)
  CartServiceTests.cs
    - add / update quantity / remove item paths
  AdminProductServiceTests.cs
    - Create / Update / Delete, variant + image sub-operations
```

### Add — mapper unit tests (`WebApp.Tests/Unit/Mappers/`)
Pure, fast, no DB. Verify `LangStr.Translate()` fallback and null-collection handling.
```
Unit/Mappers/
  ProductMapperTests.cs
  OrderMapperTests.cs
  CartMapperTests.cs
```

### Add — repository tests (`WebApp.Tests/Unit/Repositories/`)
Focus on the Include-heavy and IDOR-critical ones:
```
Unit/Repositories/
  OrderRepositoryTests.cs   - GetUserOrderById enforces userId filter
  ProductRepositoryTests.cs - GetListFiltered include graph is complete
  CartRepositoryTests.cs    - GetActiveCartForUser returns only Active status
```

### Extend — integration tests
Add to existing API integration suite:
- Cart endpoints: add-to-cart, update, remove, IDOR (user A cannot touch user B cart).
- Admin endpoints: non-admin JWT gets 403; admin CRUD round-trip on a product.
- Order placement happy path: register → add to cart → place order → 201 + order body.

### Shared test infra
Add a `TestUnitOfWorkFactory` / `TestDbContextFactory` helper under
`WebApp.Tests/Helpers/` that spins up a SQLite-in-memory `AppDbContext` + seeded data,
so service/repository tests share setup.

### Done when
- Every BLL service has at least one happy-path + one failure-path test.
- Every mapper has a test.
- IDOR is asserted at both BLL (`OrderServiceTests`) and API
  (`IntegrationTestIDAR`, extended for Cart) levels.
- `dotnet test` is green.

---

## Suggested execution order

1. **Gap 1 first** — repositories + UoW, migrate services one by one, keep tests green.
2. **Gap 2 second** — extract mappers; services are already smaller after Gap 1.
3. **Gap 3 last** — test the now-stable seams (repos, UoW, mappers, services).

Rationale: testing before the refactor means rewriting tests twice. Doing repos before
mappers means mappers are extracted from already-simplified service methods.

## Out of scope (not gaps, do not touch)
Domain entities, migrations, JWT/auth flow, Swagger, .resx translations, LangStr,
existing CI/CD (`.gitlab-ci.yml` lives two levels up in the monorepo root), admin
seeding (`App.DAL.EF/Seeding/AppDataInit.cs` — already present).
