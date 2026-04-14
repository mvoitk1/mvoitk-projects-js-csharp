## Context

ASP.NET Core (.NET 10) Todo API project with 5 API controllers, JWT Bearer authentication for Todo endpoints, API key authentication for ListItems, and a real PostgreSQL integration test setup via `CustomWebApplicationFactory<Program>`. Existing test infrastructure uses xUnit 2.9.3, `Microsoft.AspNetCore.Mvc.Testing`, NSubstitute, and AngleSharp. Tests run against a `WebApp_TESTING` PostgreSQL database that is dropped/migrated/seeded per `IClassFixture` lifetime. A single seeded admin user (`akaver@akaver.com` / `Foo.bar1`) exists. All tests use `[Collection("NonParallel")]` to serialize database access.

Current test coverage: 6 tests across 4 classes covering only a fraction of the 23 API endpoints. JWT configuration uses HS256, `ClockSkew = TimeSpan.Zero`, 600-second default expiration with an `expiresInSeconds` query parameter override. Refresh tokens rotate with a 1-minute PreviousToken grace window and 7-day expiration on new tokens.

## Goals / Non-Goals

**Goals:**
- Full integration test coverage for all 23 API endpoints across 5 controllers
- Deep JWT identity verification: multi-cycle refresh rotation, token content validation, no duplicate claims
- Verify refresh token DB records do not accumulate (no double records per session)
- Verify user data isolation across all data controllers
- Shared test infrastructure to eliminate boilerplate duplication
- Seed data for API key-authenticated ListItems controller

**Non-Goals:**
- Unit tests for controllers with mocked dependencies (integration tests are the priority)
- Testing MVC (server-rendered) controllers — only API controllers are in scope
- Modifying any production API controller logic, DTOs, or domain models
- Performance testing or load testing
- Testing Swagger/OpenAPI documentation correctness
- Testing localization or culture-specific behavior

## Decisions

### 1. Integration-first testing strategy over unit tests

All API controller tests will be integration tests using `WebApplicationFactory` against real PostgreSQL. This validates the full pipeline: routing, middleware, authentication, EF Core query translation, and database behavior.

**Rationale:** The controllers use EF Core directly (no repository abstraction), so mocking `AppDbContext` would be fragile and wouldn't catch query translation issues. The existing test infrastructure already supports this pattern. NSubstitute is available but reserved for potential future pure-logic unit tests (e.g., `IdentityExtensions.GenerateJwt()`).

### 2. Shared `ApiTestBase` static helper class (not a base class)

Create a static helper class with methods like `LoginAsync()`, `RegisterAsync()`, and `CreateAuthMessage()` that accept an `HttpClient` parameter. This is a utility class, not an inheritance-based test base class.

**Rationale:** xUnit discourages test base class inheritance patterns. A static helper avoids coupling test classes to a shared constructor and works naturally with `IClassFixture<CustomWebApplicationFactory<Program>>`. The current duplication in `TodoCategoriesControllerTest.cs:40-53` and `TokenFlow.cs:33-49` shows the same login/deserialize pattern repeated — a helper eliminates this.

### 3. `TestDataFactory` for DTO construction with sensible defaults

Static factory methods: `CreateTodoCategory(name?, sort?)`, `CreateTodoPriority(name?, sort?)`, `CreateTodoTask(name?, categoryId, priorityId)`, `CreateListItem(description?, completed?)`. Use `Guid.NewGuid()` fragments in default names to ensure uniqueness.

**Rationale:** CRUD lifecycle tests need to create entities via the API before testing GET/PUT/DELETE. Centralizing construction prevents magic strings scattered across test methods and makes tests self-documenting.

### 4. ApiKey seeding in `AppDataInit.SeedAppData()` with known constant

Add a constant `SecretKey` GUID in `InitialData` (e.g., `Guid.Parse("00000000-0000-0000-0000-000000000001")`) and seed an `ApiKey` record associated with the admin user. Expose the constant for test code to reference.

**Rationale:** `ListItemsController` validates API keys via `IsApiKeyValid()` which checks `_context.ApiKeys.Any(e => e.SecretKey == apiKey)`. Without a seeded key, no ListItems tests can function. Using a well-known constant avoids coupling tests to database queries for key retrieval.

### 5. Unique random emails per test method to avoid cross-test pollution

All tests that register users will generate emails using `$"test-{Guid.NewGuid():N}@test.com"`. The seeded admin email is only used for login tests, never for registration tests.

**Rationale:** The `CustomWebApplicationFactory` drops/recreates the database once per `IClassFixture` lifetime, not per test method. Tests within a class share database state. Random emails prevent "User already registered" conflicts and make tests order-independent.

### 6. JWT expiration testing via `expiresInSeconds` query parameter

Use `expiresInSeconds=1` for refresh cycle tests (matching the existing `TokenFlow.cs` pattern) with `Task.Delay(1500)` waits. Multi-cycle tests (3 cycles) will take ~4.5 seconds. The `ClockSkew = TimeSpan.Zero` configuration ensures predictable expiration behavior.

**Rationale:** The API already supports this parameter in Login, Register, and RefreshToken endpoints. Short-lived tokens make refresh cycle tests practical without waiting 10 minutes. The existing `TokenFlow.JwtRefreshTokenTest` validates this approach works.

### 7. Test file organization by controller/feature

```
Tests/
  Helpers/
    ApiTestBase.cs           -- static login/register/auth helper methods
    TestDataFactory.cs       -- static DTO factory methods
    DataSeeder.cs            -- enhanced with ApiKey seeding (existing file)
  Integration/
    api/
      Identity/
        AccountLoginTest.cs          -- 6 tests for POST /Account/Login
        AccountRegisterTest.cs       -- 5 tests for POST /Account/Register
        AccountRefreshTokenTest.cs   -- 9 tests for POST /Account/RefreshToken
      TodoCategoriesControllerTest.cs  -- 6 tests (extends existing 2-test file)
      TodoPrioritiesControllerTest.cs  -- 4 new tests
      TodoTasksControllerTest.cs       -- 6 new tests
      ListItemsControllerTest.cs       -- 6 new tests
      CrossCuttingTest.cs              -- 3 new tests
```

**Rationale:** Splitting identity tests into Login/Register/RefreshToken files keeps each file focused and under ~200 lines. The existing `TokenFlow.cs` test can be retired or kept alongside the more comprehensive `AccountRefreshTokenTest.cs`. TodoCategories extends the existing file to preserve backward compatibility.

### 8. Multi-cycle refresh token verification approach

The 3-cycle test will:
1. Login with `expiresInSeconds=1` → capture JWT₁, RT₁
2. Wait 1.5s → RefreshToken(JWT₁, RT₁) with `expiresInSeconds=1` → capture JWT₂, RT₂
3. Wait 1.5s → RefreshToken(JWT₂, RT₂) with `expiresInSeconds=1` → capture JWT₃, RT₃
4. Wait 1.5s → RefreshToken(JWT₃, RT₃) → capture JWT₄, RT₄
5. Assert all JWTs are distinct, all refresh tokens are distinct
6. Assert old refresh tokens (RT₁, RT₂) are no longer valid after their PreviousToken grace period expires

The no-double-records test will perform 5 cycles and after each cycle verify that a refresh using the current tokens succeeds (proving the controller's `Count != 1` assertion at `AccountController.cs:274-277` is never triggered).

### 9. JWT claim content validation strategy

Decode JWTs using `JwtSecurityTokenHandler.ReadJwtToken()` (already used in existing `TokenFlow.cs:68`). Validate:
- **After Login (seeded user):** `ClaimTypes.Email` present, `iss`/`aud` = `taltech.akaver.com`, no `ClaimTypes.GivenName` (not added via `AddClaimAsync` for seeded user)
- **After Register (new user):** `ClaimTypes.Email`, `ClaimTypes.GivenName`, `ClaimTypes.Surname` present with correct values. Assert no claim type appears more than once using `claims.GroupBy(c => c.Type).All(g => g.Count() == 1)`.
- **Across refresh cycles:** Compare claim sets between JWT₁ and JWT₂ — `sub`, `email`, `iss`, `aud` must be identical; only `exp`, `nbf`, `iat` may change.

## Risks / Trade-offs

### Test execution time
Multi-cycle refresh tests require `Task.Delay` waits (~1.5s per cycle). The 5-cycle no-double-records test will take ~7.5 seconds. Total test suite runtime increases by 30-60 seconds. This is acceptable for integration tests and matches the existing `TokenFlow` test pattern.

### Database state sharing within IClassFixture
Tests within the same `IClassFixture` share database state. Mitigated by using unique random data per test. If a test fails mid-execution and leaves orphaned data, subsequent tests are unaffected because they don't depend on specific pre-existing data (except the seeded admin user and API key).

### TodoPriorities PUT IDOR vulnerability
The `PutTodoPriority` endpoint uses `EntityState.Modified` without verifying the record belongs to the current user (`TodoPrioritiesController.cs:66`). The user isolation test will document this current behavior (PUT succeeds for another user's record, overwriting `AppUserId`). The test establishes a regression baseline with a code comment noting the known issue.

### Refresh token timing sensitivity
The PreviousToken grace period test relies on the 1-minute window at `AccountController.cs:298`. The test performs the old-token refresh immediately after rotation (well within 1 minute), making it timing-safe. The multi-cycle test's 1.5-second inter-cycle delays are also well within the grace window for adjacent cycles.

### ListItems controller uses domain entities directly (not DTOs)
`ListItemsController` accepts and returns `ListItem` domain entities directly, including the `[JsonIgnore]` properties (`ApiKeyId`, `ApiKey`). POST requests send `ListItem` with `Description` and `Completed` — the `ApiKeyId` is set server-side from the query parameter. Tests must account for this non-DTO pattern when constructing request bodies.
