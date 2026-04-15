# Implementation Plan — vue_front_to_PP

## Goal

Build a fully-functional Vue 3 e-commerce frontend that talks to the C# backend
at `https://mvoitk-cs3.proxy.itcollege.ee/api/v1`.

---

## Phase 1 — Foundation

### 1.1 HTTP client + types
- [ ] Create `src/types/index.ts` — TypeScript interfaces for every API DTO:
  - Auth: `JWTResponse`, `LoginPayload`, `RegisterPayload`, `RefreshTokenModel`
  - Products: `ProductListItemDto`, `ProductDto`, `ProductVariantDto`, `ProductImageDto`
  - Categories: `CategoryDto`
  - Collections: `CollectionDto`
  - Cart: `CartDto`, `CartItemDto`, `AddToCartDto`, `UpdateCartItemDto`
  - Orders: `OrderListItemDto`, `OrderDto`, `OrderItemDto`, `CreateOrderDto`
  - Admin DTOs (prefix `Admin`): all write/read DTOs for categories, collections, products, variants, images, orders, stock
- [ ] Create `src/api/client.ts` — base fetch wrapper:
  - reads `BASE_URL` from `import.meta.env.VITE_API_BASE`
  - attaches `Authorization: Bearer <jwt>` from localStorage when present
  - on 401: calls `RenewRefreshToken`, retries once, then dispatches a `logout` event
- [ ] Create per-resource API modules:
  - `src/api/auth.ts`
  - `src/api/products.ts`
  - `src/api/categories.ts`
  - `src/api/collections.ts`
  - `src/api/cart.ts`
  - `src/api/orders.ts`
  - `src/api/admin.ts`

### 1.2 Environment
- [ ] Create `.env` with `VITE_API_BASE=https://mvoitk-cs3.proxy.itcollege.ee/api/v1`

### 1.3 Pinia stores
- [ ] `src/stores/locale.ts` — `locale: 'en' | 'et'`, persisted to `localStorage`; composable `useLocale()` with `t(en, et)` helper
- [ ] `src/stores/auth.ts` — `jwt`, `refreshToken`, `isAdmin` (decoded from `payload.role === 'Admin'`), actions: `login`, `register`, `logout`, `renewToken`; persist to `localStorage`
- [ ] `src/stores/cart.ts` — `cart`, actions: `fetchCart`, `addItem`, `updateItem`, `removeItem`
- [ ] `src/stores/products.ts` — `list`, `current`, actions: `fetchList`, `fetchOne`
- [ ] `src/stores/categories.ts` — `tree`, action: `fetchCategories`
- [ ] `src/stores/collections.ts` — `list`, action: `fetchCollections`
- [ ] `src/stores/orders.ts` — `list`, `current`, actions: `fetchOrders`, `fetchOne`, `placeOrder`

### 1.4 Router
- [ ] Define routes in `src/router/index.ts`:

| Path | View | Auth required |
|---|---|---|
| `/` | `HomeView` | no |
| `/products` | `ProductsView` | no |
| `/products/:id` | `ProductDetailView` | no |
| `/cart` | `CartView` | yes |
| `/checkout` | `CheckoutView` | yes |
| `/orders` | `OrdersView` | yes |
| `/orders/:id` | `OrderDetailView` | yes |
| `/login` | `LoginView` | no |
| `/register` | `RegisterView` | no |
| `/admin` | `AdminLayout` | yes + admin role |
| `/admin/categories` | `AdminCategoriesView` | yes + admin |
| `/admin/collections` | `AdminCollectionsView` | yes + admin |
| `/admin/products` | `AdminProductsView` | yes + admin |
| `/admin/products/:id` | `AdminProductDetailView` | yes + admin |
| `/admin/orders` | `AdminOrdersView` | yes + admin |

- [ ] Navigation guard: redirect unauthenticated users to `/login`; redirect non-admin users away from `/admin/*`

---

## Phase 2 — Public pages

### 2.1 App shell
- [ ] `src/App.vue` — router outlet + `<NavBar />`
- [ ] `src/components/NavBar.vue`:
  - links: Home, Products, Cart (with item count badge), Login/Register or user menu
  - show/hide admin link based on role

### 2.2 Home page (`HomeView`)
- [ ] Fetch and display active collections as cards
- [ ] "Shop now" links to `/products?collectionId=...`

### 2.3 Products listing (`ProductsView`)
- [ ] Query params: `categoryId`, `collectionId`, `gender`
- [ ] Sidebar/filter panel: category tree, collections, gender options
- [ ] Product grid using `<ProductCard />` component
- [ ] `<ProductCard />` shows: primary image, name, `lowestPrice`, collection name

### 2.4 Product detail (`ProductDetailView`)
- [ ] Image gallery (sorted by `sortOrder`)
- [ ] Name, description, material, gender, collection
- [ ] Variant selector (size + color — display SKU groups)
- [ ] Quantity picker (1–100)
- [ ] "Add to cart" button → calls `cartStore.addItem`

---

## Phase 3 — Auth

### 3.1 Login (`LoginView`)
- [ ] Form: email + password
- [ ] On success: store tokens, redirect to previous route or home

### 3.2 Register (`RegisterView`)
- [ ] Form: firstName, lastName, email, password
- [ ] On success: same as login

### 3.3 Logout
- [ ] Button in NavBar → calls `POST /Account/Logout`, clears store + localStorage

---

## Phase 4 — Cart & Checkout

### 4.1 Cart (`CartView`)
- [ ] List `CartItemDto` rows: product name, variant info, unit price, quantity editor, line total, remove button
- [ ] Display cart total and item count
- [ ] "Proceed to checkout" button

### 4.2 Checkout (`CheckoutView`)
- [ ] Shipping address form (firstName, lastName, email, phone, country, city, street, postalCode)
- [ ] Order summary sidebar (items + total from cart)
- [ ] "Place order" → `POST /Orders` → redirect to `OrderDetailView`

---

## Phase 5 — Orders

### 5.1 Order list (`OrdersView`)
- [ ] Table: order number, status, total, date, item count, "View" link

### 5.2 Order detail (`OrderDetailView`)
- [ ] Shipping details section
- [ ] Line items table
- [ ] Status badge

---

## Phase 6 — Admin panel

All admin views share an `AdminLayout` with a sidebar nav.

### 6.1 Admin Categories (`AdminCategoriesView`)
- [ ] List with name (En/Et), parent, action buttons
- [ ] Create / Edit modal with `AdminCategoryWriteDto` form
- [ ] Delete with confirmation

### 6.2 Admin Collections (`AdminCollectionsView`)
- [ ] List with name (En/Et), launch date, active toggle
- [ ] Create / Edit modal
- [ ] Delete with confirmation

### 6.3 Admin Products (`AdminProductsView`)
- [ ] List with name, gender, active flag, price
- [ ] "New product" → `AdminProductDetailView` (create mode)
- [ ] Row click → `AdminProductDetailView` (edit mode)

### 6.4 Admin Product Detail (`AdminProductDetailView`)
- [ ] Product fields form (`AdminProductWriteDto`)
- [ ] Variant sub-table: SKU, price, stock, color, size — add/edit/delete rows
- [ ] Stock quick-edit: `PUT /admin/stock/{variantId}`
- [ ] Images sub-table: URL, alt text (En/Et), sort order — add/delete

### 6.5 Admin Orders (`AdminOrdersView`)
- [ ] List filterable by status
- [ ] Click row → order detail panel / modal
- [ ] Status dropdown → `PUT /admin/orders/{id}/status`

---

## Phase 7 — Polish & deployment

- [ ] Error boundary / toast notifications for API errors
- [ ] Loading spinners on async operations
- [ ] Empty-state messages (no products, empty cart, no orders)
- [ ] `vite.config.ts`: set `base` if needed for the nginx deployment at port 75
- [ ] Verify CORS headers on backend allow the frontend origin
- [ ] Production build + smoke test against the live API

---

## Resolved decisions

1. **Colors & Sizes** — Seeded in the DB; a lookup endpoint will be available. Fetch and populate dropdowns in the admin variant form.
2. **Language** — Full EN/ET switching required. Use a `locale` store (`'en' | 'et'`) and a `t(en, et)` helper that picks the right string. Language toggle in the NavBar.
3. **Image hosting** — Admin uses a URL text input only. No file upload.
4. **Pagination** — Skipped for now. Load all products in a single request.
5. **JWT role claim** — The role is stored under the `role` claim key. Decode with `JSON.parse(atob(jwt.split('.')[1]))` and check `payload.role === 'Admin'`.
