# UI Plan — Brand E-Commerce Platform (Assignment 3)

## Current State Audit

All views exist and are structurally complete. The stack is Bootstrap 5 + Alpine.js + HTMX. The issues below range from critical bugs that break functionality to minor polish items.

---

## Critical Bugs (break functionality)

### 1. Alpine.js variant selector broken on product detail
**File:** [WebApp/Views/Shop/Detail.cshtml](WebApp/Views/Shop/Detail.cshtml#L5)

`System.Text.Json.JsonSerializer.Serialize` uses PascalCase by default. The serialized variants JSON has `ColorId`, `SizeId`, `StockQty`, `ColorName`, `ColorHex`, `SizeCode`. The JavaScript function accesses `v.colorId`, `v.sizeId`, `v.stockQty`, etc. (camelCase). **All property lookups return `undefined`** — color/size selection, stock display, and Add to Cart never work.

**Fix options (pick one):**
- Add `JsonSerializerOptions` with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` to the `JsonSerializer.Serialize` call.
- Or update all JS property accesses to PascalCase.

### 2. Page title hardcoded as "Robot"
**File:** [WebApp/Views/Shared/_Layout.cshtml](WebApp/Views/Shared/_Layout.cshtml#L9)

```html
<title>@ViewData["Title"] - Robot</title>
```

Should be:
```html
<title>@ViewData["Title"] — @AppNameService.AppName</title>
```

---

## Architecture Issue (violates project rules)

### 3. Stock view exposes domain entity directly
**File:** [WebApp/Areas/Admin/Views/Stock/Index.cshtml](WebApp/Areas/Admin/Views/Stock/Index.cshtml#L3)

Model is `IEnumerable<ProductVariant>` (domain entity). CLAUDE.md says "Never expose domain entities directly." Needs a `AdminStockItemDto` and a corresponding controller change.

**Fields needed in DTO:** VariantId, ProductName, Sku, ColorName, SizeCode, StockQty.

---

## Missing UI Features

### 4. Cart count badge in navbar
**File:** [WebApp/Views/Shared/_Layout.cshtml](WebApp/Views/Shared/_Layout.cshtml#L54)

The Cart nav link shows plain text "Cart" with no item count. Users can't see cart state without navigating there. Options:
- A) Server-side: Inject `ICartService` into the layout (or a `ViewComponent`) to fetch count for authenticated users.
- B) Simpler: Store cart count in a cookie/session and read it in the layout.
- C) Minimal: Leave as-is and note it as a known limitation.

### 5. Admin: no Add Variant on Create — only on Edit
**File:** [WebApp/Areas/Admin/Views/Products/Create.cshtml](WebApp/Areas/Admin/Views/Products/Create.cshtml)

A product created via the Create form has zero variants and is therefore invisible in the shop (no prices). The user must immediately go to Edit to add variants. This is acceptable UX if the flow is documented, but ideally the Create success redirects to Edit automatically.

**Fix:** In `ProductsController.Create` POST, redirect to `Edit` rather than `Index` after creation.

### 6. Identity Login/Register pages — default ASP.NET Core UI
Default Identity Razor Pages are used. They load Bootstrap 3-era scaffolding classes and may not match Bootstrap 5 styles. Login and Register are the entry points for all users.

**Fix:** Scaffold the Identity pages into the project:
```
dotnet aspnet-codegenerator identity -dc App.DAL.EF.AppDbContext --files "Account.Login;Account.Register"
```
Then restyle them to match `_Layout.cshtml`.

---

## Per-Page Status

| Page | View File | Status | Notes |
|---|---|---|---|
| Home | Views/Home/Index.cshtml | ✅ Complete | Hero, collections, featured, categories |
| Shop Listing | Views/Shop/Index.cshtml | ✅ Complete | Filter sidebar + product grid |
| Product Detail | Views/Shop/Detail.cshtml | ⚠️ Bug | Alpine.js broken — see Critical Bug #1 |
| Cart | Views/Cart/Index.cshtml | ✅ Complete | Table + order summary sidebar |
| Checkout | Views/Checkout/Index.cshtml | ✅ Complete | Shipping form + cart summary |
| Order Success | Views/Checkout/Success.cshtml | ✅ Complete | Confirmation with order number |
| My Orders | Views/Orders/Index.cshtml | ✅ Complete | Table with status badges |
| Order Detail | Views/Orders/Detail.cshtml | ✅ Complete | Items + shipping address card |
| Admin Dashboard | Areas/Admin/Views/Dashboard/Index.cshtml | ✅ Complete | Stats + quick actions |
| Admin Products List | Areas/Admin/Views/Products/Index.cshtml | ✅ Complete | Table with badges |
| Admin Product Create | Areas/Admin/Views/Products/Create.cshtml | ⚠️ UX gap | No variant add — see Issue #5 |
| Admin Product Edit | Areas/Admin/Views/Products/Edit.cshtml | ✅ Complete | Variants + images inline |
| Admin Categories | Areas/Admin/Views/Categories/Index.cshtml | ✅ Complete | Bilingual names + CRUD |
| Admin Category Create/Edit | Areas/Admin/Views/Categories/Create+Edit | ✅ Complete | |
| Admin Collections | Areas/Admin/Views/Collections/ | ✅ Complete | With shared `_CollectionForm` partial |
| Admin Orders List | Areas/Admin/Views/Orders/Index.cshtml | ✅ Complete | Status filter dropdown |
| Admin Order Detail | Areas/Admin/Views/Orders/Detail.cshtml | ✅ Complete | Status update form |
| Admin Stock | Areas/Admin/Views/Stock/Index.cshtml | ⚠️ Architecture | Domain entity exposed — see Issue #3 |
| Shared Layout | Views/Shared/_Layout.cshtml | ⚠️ Bug | Hardcoded "Robot" title — see Critical Bug #2 |
| Admin Layout | Areas/Admin/Views/Shared/_AdminLayout.cshtml | ✅ Complete | Dark sidebar, active nav |
| Login/Register | Areas/Identity/ (default UI) | ⚠️ Needs scaffold | May not match Bootstrap 5 styles |

---

## Implementation Priority Order

### P0 — Fix before anything else
1. Fix Alpine.js camelCase serialization bug (Detail.cshtml)
2. Fix "Robot" title in `_Layout.cshtml`

### P1 — Architecture/rules compliance
3. Create `AdminStockItemDto`, update `StockController` and `Stock/Index.cshtml` to use it

### P2 — UX gaps
4. Redirect Product Create POST → Edit instead of Index
5. Scaffold + restyle Login/Register Identity pages

### P3 — Polish
6. Cart count badge in navbar (ViewComponent approach recommended)
7. Move admin layout inline CSS to a dedicated `admin.css` file
8. Add `TempData` success/error alerts to admin CRUD operations (products, categories, collections currently give no feedback on success)
9. Responsive: verify mobile navbar collapse works (hamburger menu)

---

## Questions

1. **Identity pages**: Is default ASP.NET Identity UI acceptable as-is, or does Login/Register need to match the shop's style? (scaffolding and restyling takes meaningful effort)
2. **Cart badge**: How important is showing cart count in the navbar? It requires either a ViewComponent (clean) or session/cookie (simpler but less accurate).
3. **Existing data / seed**: Is there already data in the database, or will we be working against a fresh seed? (affects whether we can test the full flow immediately)
4. **Confirmation flow for product create**: Should Create redirect to Edit (so variants can be added immediately), or is a two-step flow acceptable?
