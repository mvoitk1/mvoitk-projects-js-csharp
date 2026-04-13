# DOCUMENTATION.md — Brand E-Commerce Platform

## Project summary

Single-brand fashion e-commerce web platform.  
Backend: ASP.NET Core 8, PostgreSQL, Entity Framework Core, JWT authentication.  
Frontend: Razor MVC (server-rendered), HTMX for lightweight interactivity.  
Deployment: Single Docker container behind Nginx on VPS.

---

## Domain model

### Entities in scope (Phase 1)

#### Identity
| Entity | Key fields | Notes |
|---|---|---|
| `AppUser` | Id (Guid), FirstName, LastName, Email, Phone | Extends IdentityUser<Guid> |
| `AppRole` | Id (Guid), Name | Extends IdentityRole<Guid> |
| `AppRefreshToken` | Id, Token, ExpiresAt, AppUserId (FK) | JWT refresh flow |

#### Catalogue
| Entity | Key fields | Notes |
|---|---|---|
| `Category` | Id, Name (LangStr/jsonb), ParentCategoryId? (self-ref FK) | Hierarchical (e.g. Men → T-Shirts) |
| `Collection` | Id, Name (LangStr/jsonb), Description (LangStr/jsonb), LaunchDate, IsActive | Seasonal / limited collections |
| `Product` | Id, Name (LangStr/jsonb), Description (LangStr/jsonb), Material (LangStr/jsonb), Gender (enum), IsActive, CreatedAt, CollectionId? (FK) | Base product |
| `ProductCategory` | ProductId (FK), CategoryId (FK), From, Until | Many-to-many junction with time range |
| `ProductImage` | Id, Url, AltText (LangStr/jsonb), SortOrder, ProductId (FK) | Image URL (no file upload in Phase 1) |
| `Color` | Id, Name (LangStr/jsonb), HexCode | |
| `Size` | Id, SizeCode, DisplayName (LangStr/jsonb) | e.g. SizeCode="XL", DisplayName="Extra Large" |
| `ProductVariant` | Id, Sku, Price, UnitPrice, StockQty, IsActive, ColorId (FK), SizeId (FK), ProductId (FK) | Each size+color combo is a separate SKU |

#### Cart & Orders
| Entity | Key fields | Notes |
|---|---|---|
| `Cart` | Id, Status, CreatedAt, UpdatedAt, AppUserId (FK) | One active cart per user |
| `CartItem` | Id, Quantity, UnitPrice, CartId (FK), ProductVariantId (FK) | |
| `Order` | Id, OrderNumber, Status (enum), UnitPrice, TotalAmount, ShippingAddress (plain text fields), CreatedAt, AppUserId (FK) | ShippingAddress stored as flat fields (Phase 1) |
| `OrderItem` | Id, Quantity, UnitPrice, LineTotal, OrderId (FK), ProductVariantId (FK) | Snapshot price at time of order |

### LangStr — translatable fields

`LangStr` is stored as PostgreSQL `jsonb`. It is a dictionary keyed by 2-letter culture code (`"en"`, `"et"`).

Translatable fields:
- `Product`: Name, Description, Material
- `Category`: Name
- `Collection`: Name, Description
- `Color`: Name
- `Size`: DisplayName
- `ProductImage`: AltText

Plain (non-translated) fields: SKU, prices, HexCode, SizeCode, OrderNumber, user personal data, all enum values, all timestamps.

### Gender enum
```
NotSpecified = 0
Men = 1
Women = 2
Unisex = 3
```

### Order status enum
```
Pending = 0
Confirmed = 1
Processing = 2
Shipped = 3
Delivered = 4
Cancelled = 5
```

### Cart status enum
```
Active = 0
CheckedOut = 1
Abandoned = 2
```

---

## Architecture layers

```
App.Domain      — entities, enums, LangStr, IBaseEntity, BaseEntity, Identity models
App.DAL.EF      — AppDbContext (DbSets, OnModelCreating), EF migrations, seed data
App.BLL         — service interfaces + implementations:
                    IProductService / ProductService
                    ICategoryService / CategoryService
                    ICollectionService / CollectionService
                    ICartService / CartService
                    IOrderService / OrderService
                    IAdminProductService / AdminProductService
                    IAdminOrderService / AdminOrderService
App.DTO         — DTOs in v1/:
                    Product: ProductDto, ProductListItemDto, ProductVariantDto
                    Cart: CartDto, CartItemDto, AddToCartDto
                    Order: OrderDto, OrderListItemDto, CreateOrderDto
                    Auth: LoginDto, RegisterDto, RefreshTokenDto, JwtResponseDto
                    Admin: AdminProductDto, AdminOrderDto (with write models)
App.Helpers     — JsonHelpers (LangStr serialisation support)
App.Resources   — .resx for View string translations (en + et)
Base.Resources  — Common.resx / Common.et.resx (shared button labels, error messages)
WebApp          — ASP.NET Core host
```

### Dependency direction
`WebApp → App.BLL → App.DAL.EF → App.Domain`  
`App.DTO` is referenced by `App.BLL` and `WebApp`.  
`App.Helpers` / `App.Resources` / `Base.Resources` are referenced where needed.

---

## REST API

Base path: `/api/v1/`  
Auth: Bearer JWT in `Authorization` header.  
All responses: `application/json`. All list responses are arrays (no envelope).

### Versioning
Implemented via Asp.Versioning. Version specified in URL segment (`/api/v1/`).  
Swagger UI available at `/swagger`.

### Endpoints (Phase 1)

#### Auth — `/api/v1/account`
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/register` | — | Register new customer account |
| POST | `/login` | — | Login, returns JWT + refresh token |
| POST | `/refresh-token` | — | Exchange refresh token for new JWT |
| POST | `/logout` | JWT | Revoke refresh token |

#### Products — `/api/v1/products`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | — | List products (filter by category, collection, gender) |
| GET | `/{id}` | — | Product detail with variants and images |

#### Categories — `/api/v1/categories`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | — | List all categories (hierarchical) |

#### Collections — `/api/v1/collections`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | — | List active collections |

#### Cart — `/api/v1/cart`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | JWT | Get current user's active cart |
| POST | `/items` | JWT | Add item to cart |
| PUT | `/items/{cartItemId}` | JWT | Update quantity |
| DELETE | `/items/{cartItemId}` | JWT | Remove item |

#### Orders — `/api/v1/orders`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | JWT | List current user's orders |
| GET | `/{id}` | JWT | Order detail (IDOR: user must own the order) |
| POST | `/` | JWT | Place order from active cart |

#### Admin — `/api/v1/admin/...`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET/POST/PUT/DELETE | `/products` | Admin JWT | Full product CRUD incl. variants |
| GET/POST/PUT/DELETE | `/categories` | Admin JWT | Category CRUD |
| GET/POST/PUT/DELETE | `/collections` | Admin JWT | Collection CRUD |
| GET | `/orders` | Admin JWT | List all orders |
| PUT | `/orders/{id}/status` | Admin JWT | Update order status |
| PUT | `/products/{id}/variants/{variantId}/stock` | Admin JWT | Update stock quantity |

---

## MVC views (UX)

### Customer area (default)
| Route | Controller/Action | Description |
|---|---|---|
| `/` | Home/Index | Landing page: hero banner, featured products, categories |
| `/shop` | Shop/Index | Product listing with category/collection/gender filter |
| `/shop/{id}` | Shop/Detail | Product detail: images, variant selector, add to cart |
| `/cart` | Cart/Index | Cart view: items, quantities, totals |
| `/checkout` | Checkout/Index | Checkout form: shipping address, order summary |
| `/checkout/confirm` | Checkout/Confirm | POST: place order, redirect to success |
| `/orders` | Orders/Index | Order history (requires login) |
| `/orders/{id}` | Orders/Detail | Order detail (requires login, IDOR enforced) |
| `/account/login` | Account/Login | Login form |
| `/account/register` | Account/Register | Registration form |
| `/account/logout` | Account/Logout | Logout |

### Admin area (`/Admin/`)
| Route | Description |
|---|---|
| `/Admin` | Dashboard: counts of products, orders, low-stock alerts |
| `/Admin/Products` | Product list |
| `/Admin/Products/Create` | Create product + variants |
| `/Admin/Products/Edit/{id}` | Edit product + variants + images |
| `/Admin/Products/Delete/{id}` | Delete product |
| `/Admin/Categories` | Category CRUD |
| `/Admin/Collections` | Collection CRUD |
| `/Admin/Orders` | Order list with status filter |
| `/Admin/Orders/Detail/{id}` | Order detail + status change |
| `/Admin/Stock` | Variant stock management |

---

## Authentication & authorisation

- JWT issued on login, short-lived (15 min default).
- Refresh token stored in `AppRefreshToken` table, long-lived (7 days).
- `Admin` role assigned via seeding to the admin seed user.
- Admin MVC area protected by `[Authorize(Roles = "Admin")]` on area controller base.
- IDOR enforcement: Cart and Order BLL methods always filter by the `AppUserId` extracted from `HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)`. A user requesting another user's order gets 403/404.

---

## Translations

### UI (resource files)
Location: `App.Resources/Views/` (mirrors MVC view folder structure).  
Files: `*.resx` (English default) + `*.et.resx` (Estonian).  
`Base.Resources/Common.resx` + `Common.et.resx` for shared strings (buttons, labels).  
Language switch: query param `?culture=et` or cookie.

### Database (LangStr)
Stored as `jsonb` in PostgreSQL.  
Resolved at query time using `LangStr.Translate()` with current `Thread.CurrentUICulture`.  
Admin forms show separate input fields per language (en + et) for each LangStr field.

---

## Deferred features (not implemented in Phase 1)

> These must be implemented before a production launch. Each item has been deliberately excluded from Phase 1 to keep scope manageable for the assignment deadline.

### Payment processing
- **What:** Integration with Estonian bank payment links (Swedbank, SEB, LHV, Luminor) and card payments (Stripe or similar).
- **Why deferred:** Requires merchant account setup, webhook handling, and PCI compliance considerations beyond academic scope.
- **Hook point:** `IPaymentService` interface should be added in `App.BLL`. `OrderService.PlaceOrder()` currently skips payment and sets status to `Confirmed` directly. Replace this call with a payment initiation step.

### Product reviews
- **What:** `Review` entity (Rating, Comment, CreatedAt, ProductId FK, AppUserId FK). Customer can submit one review per purchased product. Public listing on product detail page.
- **Why deferred:** Non-critical for core shopping flow.
- **Hook point:** `Review` entity is defined in ERD. Add DbSet, migration, ReviewService, and review sub-section in product detail view.

### Address book
- **What:** `Address` entity (Type, Country, City, Street, PostalCode, AppUserId FK). Users save multiple addresses, select one at checkout.
- **Why deferred:** Phase 1 uses flat text fields on Order for shipping address (entered fresh each checkout).
- **Hook point:** `Address` entity is modelled in ERD. Add DbSet, migration, AddressService, and address selector in checkout view.

### Email notifications
- **What:** Order confirmation email to customer; new order notification to admin.
- **Why deferred:** Requires SMTP or transactional email provider (SendGrid/Mailgun) setup.
- **Hook point:** Add `IEmailService` in App.BLL. Call from `OrderService.PlaceOrder()` after order is persisted.

### Product image file upload
- **What:** Admin can upload image files; stored in object storage or local volume.
- **Why deferred:** Phase 1 admin form accepts image URL strings only.
- **Hook point:** Replace the URL string input in the admin product form with a file upload input. Add `IStorageService` in App.BLL.

### Wishlist
- **Not in ERD. Future feature.** Users save products for later. Requires a `Wishlist` / `WishlistItem` entity pair.

### Stock reservation on cart add
- **What:** Reserve stock when item is added to cart (not just when order is placed). Requires a stock reservation table and a background job to release expired reservations.
- **Why deferred:** Phase 1 checks stock only at order placement.

---

## CI/CD

### GitLab CI
CI config location: `../../.gitlab-ci.yml` (repo root, two levels above this folder).  
Add a `deploy_csharp_a3` job following the same pattern as `deploy_csharp_a1`:

```yaml
deploy_csharp_a3:
  stage: deploy
  only:
    - main
  tags:
    - shared
  script:
    - cd "Csharp/Assignment_3"
    - printf "APP_PORT=%s\nASPNETCORE_ENVIRONMENT=%s\nPOSTGRES_DB=%s\nPOSTGRES_USER=%s\nPOSTGRES_PASSWORD=%s\n" \
        "${CSHARP_A3_APP_PORT:-82}" \
        "${CSHARP_A3_ASPNETCORE_ENVIRONMENT:-Production}" \
        "${CSHARP_A3_POSTGRES_DB:-csharp_a3}" \
        "${CSHARP_A3_POSTGRES_USER:-madis}" \
        "${CSHARP_A3_POSTGRES_PASSWORD}" > .env
    - docker compose -p csharp_a3 up --build --remove-orphans --detach
```

Required GitLab CI variables (set in GitLab project settings):
- `CSHARP_A3_APP_PORT` (default: 82)
- `CSHARP_A3_ASPNETCORE_ENVIRONMENT` (default: Production)
- `CSHARP_A3_POSTGRES_DB`
- `CSHARP_A3_POSTGRES_USER`
- `CSHARP_A3_POSTGRES_PASSWORD`

### Docker Compose
`docker-compose.yml` at `Csharp/Assignment_3/` with two services:
- `webapp` — builds from Dockerfile, exposes `APP_PORT`
- `db` — PostgreSQL 16, data volume, internal network only

Nginx on VPS proxies `APP_PORT` under the assigned domain/subdomain.

---

## Development setup

```bash
# Install/update tools
dotnet tool update -g dotnet-ef
dotnet tool update -g dotnet-aspnet-codegenerator

# Add JS libs (HTMX, Alpine)
cd WebApp && libman install htmx.org --files dist/htmx.min.js
libman install alpinejs --files dist/cdn.min.js

# Database (from solution root)
dotnet ef migrations --project App.DAL.EF --startup-project WebApp add Initial
dotnet ef database   --project App.DAL.EF --startup-project WebApp update

# Run
dotnet run --project WebApp
```

`appsettings.Development.json` must have:
- `ConnectionStrings:DefaultConnection` — local PostgreSQL
- `JWT:Key`, `JWT:Issuer`, `JWT:Audience`
- `DataInitialization:MigrateDatabase: true`
- `DataInitialization:SeedIdentity: true`
- `DataInitialization:SeedData: true`
