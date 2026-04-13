# IMPLEMENTATION_PLAN.md — Brand E-Commerce Platform

Implementation order follows a bottom-up dependency chain: domain first, then data access, then business logic, then API, then MVC views.

Check off steps as you go. Each step should be independently buildable/testable before moving on.

---

## Phase 1 — Foundation

### Step 1 — Domain entities
**Project:** `App.Domain`

- [ ] Extend `AppUser` with `FirstName`, `LastName`, `Phone` fields
- [ ] Add enum files: `Gender.cs`, `OrderStatus.cs`, `CartStatus.cs`
- [ ] Add entity: `Category.cs` (Id, Name/LangStr, ParentCategoryId?)
- [ ] Add entity: `Collection.cs` (Id, Name/LangStr, Description/LangStr, LaunchDate?, IsActive)
- [ ] Add entity: `Color.cs` (Id, Name/LangStr, HexCode)
- [ ] Add entity: `Size.cs` (Id, SizeCode, DisplayName/LangStr)
- [ ] Add entity: `Product.cs` (Id, Name/LangStr, Description/LangStr, Material/LangStr, Gender, IsActive, CreatedAt, CollectionId?)
- [ ] Add entity: `ProductCategory.cs` (ProductId FK, CategoryId FK, From, Until) — junction table
- [ ] Add entity: `ProductImage.cs` (Id, Url, AltText/LangStr, SortOrder, ProductId FK)
- [ ] Add entity: `ProductVariant.cs` (Id, Sku, Price, UnitPrice, StockQty, IsActive, ColorId FK, SizeId FK, ProductId FK)
- [ ] Add entity: `Cart.cs` (Id, Status/CartStatus, CreatedAt, UpdatedAt, AppUserId FK)
- [ ] Add entity: `CartItem.cs` (Id, Quantity, UnitPrice, CartId FK, ProductVariantId FK)
- [ ] Add entity: `Order.cs` (Id, OrderNumber, Status/OrderStatus, TotalAmount, ShippingFirstName, ShippingLastName, ShippingEmail, ShippingPhone, ShippingCountry, ShippingCity, ShippingStreet, ShippingPostalCode, CreatedAt, AppUserId FK)
- [ ] Add entity: `OrderItem.cs` (Id, Quantity, UnitPrice, LineTotal, OrderId FK, ProductVariantId FK)

**Notes:**
- All entities inherit `BaseEntity` (Guid PK).
- All `LangStr` properties: `[Column(TypeName = "jsonb")]`.
- `ProductCategory` has a composite PK (ProductId + CategoryId) — configure in `OnModelCreating`.

---

### Step 2 — Database context and migrations
**Project:** `App.DAL.EF`

- [ ] Add `DbSet<T>` for every new entity in `AppDbContext`
- [ ] `OnModelCreating`: configure `ProductCategory` composite PK; configure `Category` self-referencing FK (no cascade); configure all other restrict-delete FKs (already done globally)
- [ ] Configure `LangStr` properties as `jsonb` (verify NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson is called)
- [ ] Add initial migration: `dotnet ef migrations ... add Initial`
- [ ] Add seed data in `App.DAL.EF/Seeding/AppDataInit.cs`:
  - Admin role (`Admin`) and admin user (`admin@shop.ee` / configurable password)
  - Two sizes (S, M), two colors (Black, White)
  - One category (Tops), one collection (Spring 2026)
  - One product with two variants (one per color, size S)

---

### Step 3 — DTOs
**Project:** `App.DTO/v1/`

Create DTO files grouped by domain area:

- [ ] `Auth/` — `LoginDto`, `RegisterDto`, `RefreshTokenDto`, `JwtResponseDto`
- [ ] `Products/` — `ProductListItemDto`, `ProductDto`, `ProductVariantDto`, `ProductImageDto`
- [ ] `Categories/` — `CategoryDto`
- [ ] `Collections/` — `CollectionDto`
- [ ] `Cart/` — `CartDto`, `CartItemDto`, `AddToCartDto`, `UpdateCartItemDto`
- [ ] `Orders/` — `OrderDto`, `OrderListItemDto`, `OrderItemDto`, `CreateOrderDto`
- [ ] `Admin/` — `AdminProductDto`, `AdminProductWriteDto`, `AdminVariantDto`, `AdminVariantWriteDto`, `AdminOrderDto`, `AdminOrderStatusDto`, `AdminCategoryDto`, `AdminCategoryWriteDto`, `AdminCollectionDto`, `AdminCollectionWriteDto`, `AdminStockUpdateDto`

**Rule:** DTOs expose translated strings (call `LangStr.Translate()` in BLL mapping), not the raw `LangStr` object.

---

### Step 4 — BLL service interfaces and implementations
**Project:** `App.BLL`

Reference `App.DAL.EF` and `App.DTO`. Inject `AppDbContext` directly (no repository abstraction required).

- [ ] `IProductService` / `ProductService`:
  - `GetListAsync(categoryId?, collectionId?, gender?)` → `IEnumerable<ProductListItemDto>`
  - `GetByIdAsync(id)` → `ProductDto?`
- [ ] `ICategoryService` / `CategoryService`:
  - `GetAllAsync()` → `IEnumerable<CategoryDto>`
- [ ] `ICollectionService` / `CollectionService`:
  - `GetActiveAsync()` → `IEnumerable<CollectionDto>`
- [ ] `ICartService` / `CartService`:
  - `GetOrCreateCartAsync(userId)` → `CartDto`
  - `AddItemAsync(userId, dto)` → `CartDto` (checks stock)
  - `UpdateItemAsync(userId, cartItemId, dto)` → `CartDto` (IDOR)
  - `RemoveItemAsync(userId, cartItemId)` (IDOR)
- [ ] `IOrderService` / `OrderService`:
  - `PlaceOrderAsync(userId, dto)` → `OrderDto` (deducts stock, marks cart CheckedOut, generates OrderNumber)
  - `GetUserOrdersAsync(userId)` → `IEnumerable<OrderListItemDto>`
  - `GetUserOrderByIdAsync(userId, orderId)` → `OrderDto?` (IDOR: returns null if not owner)
- [ ] `IAdminProductService` / `AdminProductService`:
  - Full CRUD for Product, ProductVariant, ProductImage, ProductCategory assignment
- [ ] `IAdminOrderService` / `AdminOrderService`:
  - `GetAllOrdersAsync(statusFilter?)` → list
  - `UpdateStatusAsync(orderId, status)`
- [ ] `IAdminCatalogueService` / `AdminCatalogueService`:
  - CRUD for Category, Collection, Color, Size

Register all services in `WebApp/Setup/` extension method (e.g. `AddAppServices()`), called from `Program.cs`.

---

## Phase 2 — REST API

### Step 5 — API controllers
**Project:** `WebApp/ApiControllers/v1/`

- [ ] `ProductsController` — GET list, GET by id (anonymous)
- [ ] `CategoriesController` — GET all (anonymous)
- [ ] `CollectionsController` — GET active (anonymous)
- [ ] `CartController` — GET, POST item, PUT item, DELETE item (JWT required)
- [ ] `OrdersController` — GET list, GET by id, POST (JWT required)
- [ ] `Admin/ProductsController` — full CRUD (Admin role required)
- [ ] `Admin/CategoriesController` — CRUD (Admin role)
- [ ] `Admin/CollectionsController` — CRUD (Admin role)
- [ ] `Admin/OrdersController` — GET all, PUT status (Admin role)
- [ ] `Admin/StockController` — PUT stock (Admin role)
- [ ] `Identity/AccountController` — already scaffolded, extend as needed

All controllers: return `ActionResult<T>`, use 201/204 correctly, validate with `ModelState`.

---

## Phase 3 — MVC views

### Step 6 — Customer MVC
**Project:** `WebApp/Controllers/` + `WebApp/Views/`

Build views in this order (matches user journey):

- [ ] `HomeController` / `Index` view — hero, featured products (6 items), category grid
- [ ] `ShopController` / `Index` view — product grid, sidebar category/collection filter
- [ ] `ShopController` / `Detail` view — product images, variant selector (color + size), Add to Cart button (HTMX post)
- [ ] `CartController` / `Index` view — cart items table, quantity controls (HTMX), totals, Checkout button
- [ ] `CheckoutController` / `Index` view — shipping address form, order summary panel
- [ ] `CheckoutController` / `Confirm` POST action — calls `IOrderService.PlaceOrderAsync`, redirects to success page
- [ ] `OrdersController` / `Index` view — order history list (requires login)
- [ ] `OrdersController` / `Detail` view — order detail (requires login, IDOR)
- [ ] `AccountController` — Login/Register/Logout MVC forms (calls API internally or uses cookie auth directly)

**Note on auth in MVC:** Use ASP.NET Core cookie auth for MVC session (separate from JWT). On login, validate credentials via `SignInManager`, issue cookie. JWT is for the REST API only. Both auth schemes coexist via multi-scheme setup.

---

### Step 7 — Admin MVC
**Project:** `WebApp/Areas/Admin/`

- [ ] `AdminBaseController` — abstract, `[Area("Admin")]`, `[Authorize(Roles = "Admin")]`
- [ ] `DashboardController` / `Index` — counts: products, orders today, low-stock variants (<5)
- [ ] `ProductsController` — list, create, edit (with variant sub-form), delete
- [ ] `CategoriesController` — list, create, edit, delete (with parent selector)
- [ ] `CollectionsController` — list, create, edit, delete
- [ ] `OrdersController` — list (filterable by status), detail + status dropdown
- [ ] `StockController` — list variants with stock, inline edit quantity

Admin views: use a shared `_AdminLayout.cshtml` with sidebar navigation.

---

## Phase 4 — Translations

### Step 8 — UI resource files
**Project:** `App.Resources/Views/`

- [ ] Create `*.resx` + `*.et.resx` pairs for each view area:
  - `Home/Index.resx`
  - `Shop/Index.resx`, `Shop/Detail.resx`
  - `Cart/Index.resx`
  - `Checkout/Index.resx`
  - `Orders/Index.resx`, `Orders/Detail.resx`
  - `Admin/Dashboard/Index.resx`
  - `Admin/Products/Index.resx`, etc.
- [ ] Update `Base.Resources/Common.resx` + `Common.et.resx` with shared labels (Add to Cart, Checkout, Save, Cancel, Delete, etc.)
- [ ] Add language switcher partial view (`_LanguageSwitcher.cshtml`) in shared layout, using `?culture=en` / `?culture=et`

---

## Phase 5 — Infrastructure

### Step 9 — Docker & deployment

- [ ] Create `Dockerfile` in `Csharp/Assignment_3/`:
  ```dockerfile
  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  # ... copy solution, restore, publish
  FROM mcr.microsoft.com/dotnet/aspnet:8.0
  # ... copy publish output
  ENTRYPOINT ["dotnet", "WebApp.dll"]
  ```
- [ ] Create `docker-compose.yml`:
  - `webapp` service (build from Dockerfile, env vars from `.env`, depends on `db`)
  - `db` service (postgres:16-alpine, volume, internal network)
- [ ] Create `.dockerignore`
- [ ] Add `deploy_csharp_a3` job to `../../.gitlab-ci.yml` (see DOCUMENTATION.md for snippet)
- [ ] Set GitLab CI variables in GitLab project settings
- [ ] Configure Nginx on VPS for the new port/domain

### Step 10 — Tests
**Project:** `WebApp.Tests`

- [ ] Integration test: POST `/api/v1/account/login` returns JWT
- [ ] Integration test: GET `/api/v1/products` returns 200
- [ ] Integration test: GET `/api/v1/orders/{othersId}` as different user returns 403/404 (IDOR test)
- [ ] Integration test: POST `/api/v1/orders` places order and reduces stock

---

## Deferred — document only, do not implement in Phase 1

| Feature | Tracking issue |
|---|---|
| Payment processing (bank links + Stripe) | See DOCUMENTATION.md → Deferred |
| Product reviews (Review entity + UI) | See DOCUMENTATION.md → Deferred |
| Address book (saved addresses) | See DOCUMENTATION.md → Deferred |
| Email notifications (order confirmation) | See DOCUMENTATION.md → Deferred |
| Product image file upload | See DOCUMENTATION.md → Deferred |
| Wishlist | See DOCUMENTATION.md → Deferred |
| Stock reservation on cart add | See DOCUMENTATION.md → Deferred |

---

## Dependency map (what blocks what)

```
Step 1 (Domain)
  └── Step 2 (DAL/Migrations)
        └── Step 3 (DTOs) ← can be done in parallel with Step 2
              └── Step 4 (BLL)
                    ├── Step 5 (API controllers)
                    ├── Step 6 (Customer MVC)
                    └── Step 7 (Admin MVC)
                          └── Step 8 (Translations) ← can be layered in during Steps 6/7
Step 9 (Docker) ← can be set up any time after Step 2
Step 10 (Tests) ← after Step 5
```
