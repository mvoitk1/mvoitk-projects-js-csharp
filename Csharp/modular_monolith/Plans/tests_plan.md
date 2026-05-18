# Tests Plan — Filling the Gaps

This plan covers test types **not yet implemented** in `WebApp.Tests`. Each section lists the goal, where to put files, and concrete cases to write.

Existing stack to reuse: xUnit, Moq, EF Core InMemory, `WebApplicationFactory<Program>` via `CustomWebApplicationFactory`, AngleSharp, `coverlet.collector`.

---

## 1. Localization tests (en / et)

**Goal:** Verify `LangStr` JSON fields and `.resx`-backed view strings respond to `Accept-Language`.

**Location:** `WebApp.Tests/Integration/Localization/`

**Cases:**
- `LangStrTests.cs`
  - `GET /api/v1/products/{id}` with `Accept-Language: en` returns English `Name`/`Description`.
  - Same request with `Accept-Language: et` returns Estonian variants.
  - Falling back when a translation is missing returns the default culture value, not null.
- `ViewLocalizationTests.cs`
  - Home page rendered with `et` culture contains Estonian strings from `App.Resources` / `Base.Resources`.
  - Cookie-based culture switch (`?culture=et`) persists across requests.

**Setup:** Seed one product with both `en` and `et` entries in `DataSeeder`.

---

## 2. Swagger / API contract tests

**Goal:** Catch breakage in API versioning, DTO shape, and Swagger generation.

**Location:** `WebApp.Tests/Integration/Contract/`

**Cases:**
- `SwaggerTests.cs`
  - `GET /swagger/v1/swagger.json` returns 200 and valid JSON.
  - Document contains every controller under `ApiControllers/v1/`.
  - No domain entity types leak into `components.schemas` (only `App.DTO.v1.*`).
- `DtoShapeTests.cs`
  - For each public endpoint hit in integration tests, assert response JSON keys match the DTO via snapshot (or `JsonNode` key set comparison).

---

## 3. Razor view-rendering tests (beyond Home)

**Goal:** Verify customer + admin MVC pages render without server errors and contain expected markers.

**Location:** `WebApp.Tests/Integration/Views/`

**Cases (one file per controller):**
- `ShopViewTests.cs` — `/Shop`, `/Shop/Product/{id}` return 200 and contain product name marker (AngleSharp selector).
- `CartViewTests.cs` — empty cart and cart-with-items both render; quantity input present.
- `CheckoutViewTests.cs` — anonymous user is redirected to login; authenticated user sees address form.
- `AccountViewTests.cs` — login, register, profile pages render; anti-forgery token present in form.
- `Areas/Admin/AdminProductsViewTests.cs` — admin role required; non-admin gets 403; admin sees product list table.

**Helpers:** Reuse `HtmlHelpers` / AngleSharp; add an `AssertHasFormToken` helper.

---

## 4. End-to-end (Playwright)

**Goal:** Validate the two golden flows in a real browser against the deployed container.

**Location:** new project `WebApp.E2E/` (separate from xUnit; Node + Playwright).

**Why separate:** E2E is slow and flaky; should run on demand, not on every `dotnet test`.

**Flows:**
- `customer-checkout.spec.ts`
  - Browse shop → open product → add to cart → log in → fill address → place order → see order confirmation.
- `admin-product-crud.spec.ts`
  - Log in as admin → navigate to Admin/Products → create product with variants → edit → delete → verify list.

**CI:** Add `e2e_csharp_a3` GitLab job that runs after `deploy_csharp_a3`, pointed at `http://192.168.181.91:82`. Allowed to fail initially (manual trigger).

---

## 5. Smoke / health endpoint

**Goal:** Cheap post-deploy check that the app is alive and DB is reachable.

**Implementation:**
1. Add `MapHealthChecks("/health")` in `WebApp/Program.cs` with `AddDbContextCheck<AppDbContext>()`.
2. `WebApp.Tests/Integration/HealthTests.cs` — assert `/health` returns 200 with `"Healthy"` body.
3. CI: add `curl --fail http://host:82/health` step in `deploy_csharp_a3` after container start.

---

## 6. Migration-apply test

**Goal:** Catch broken migrations before deploy. Current InMemory tests never exercise EF migrations.

**Location:** `WebApp.Tests/Integration/Migrations/MigrationApplyTests.cs`

**Approach:** Use Testcontainers for PostgreSQL (`Testcontainers.PostgreSql` NuGet).

**Cases:**
- Spin up empty Postgres container → `await db.Database.MigrateAsync()` → assert no exception.
- After migrate, run `AppDataInit.SeedData` → assert seed succeeds.
- Optional: snapshot the resulting schema (table + column names) to detect accidental drift.

**Trade-off:** Adds Docker dependency to CI; gate behind a `[Trait("Category", "RequiresDocker")]` filter so local fast-runs can skip it.

---

## 7. Misc gaps worth adding while we're here

- **JWT expiry / refresh rotation tests** in `IntegrationTestIdentity.cs` — expired access token rejected; refresh issues new pair; reused refresh token rejected.
- **Decimal precision** — assert a product priced at `19.99` round-trips through API as `19.99`, not `19.9900000001`.
- **Concurrency** — two parallel `AddToCart` calls for the same variant don't double-decrement stock (if stock tracking exists).

---

## Suggested implementation order

1. Health endpoint + smoke (smallest, unblocks CI confidence).
2. Migration-apply test (highest bug-catching value pre-deploy).
3. Localization tests (cheap, validates a core feature of the assignment).
4. Swagger contract tests.
5. Razor view tests for the remaining controllers.
6. E2E (last — most setup cost, lowest per-hour bug yield).

---

## Out of scope

- Load / performance testing.
- Mutation testing (Stryker.NET) — useful but heavy; revisit only if coverage looks misleadingly high.
- Visual regression for views.
