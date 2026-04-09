# API Controller Test Coverage Plan

## Objective

Achieve comprehensive unit and integration test coverage for all 5 API controllers (`AccountController`, `TodoCategoriesController`, `TodoPrioritiesController`, `TodoTasksController`, `ListItemsController`) with special emphasis on JWT identity verification including multi-cycle refresh token flows, JWT content validation, and protection against duplicate records in token storage.

## Current State Assessment

### Existing Coverage (6 tests across 4 classes)
| Test Class | Tests | What's Covered |
|---|---|---|
| `HomeControllerTest` | 1 | Smoke test for homepage |
| `TodoCategoriesControllerTest` | 2 | 401 without auth + GET list with JWT |
| `TokenFlow` | 1 | Single login + single refresh cycle |
| `RegistrationFlow` | 2 | MVC form registration + redirect check |

### Coverage Gaps (by endpoint)
- **AccountController**: No register API test, no login failure tests, no multi-cycle refresh, no JWT content validation
- **TodoCategoriesController**: Missing GET by ID, POST, PUT, DELETE tests
- **TodoPrioritiesController**: Zero tests
- **TodoTasksController**: Zero tests
- **ListItemsController**: Zero tests (uses API key auth, different pattern)

### Test Infrastructure Available
- **Framework**: xUnit 2.9.3, `Microsoft.AspNetCore.Mvc.Testing`, NSubstitute, AngleSharp
- **Factory**: `CustomWebApplicationFactory<Program>` using real PostgreSQL (`WebApp_TESTING`)
- **Seeded User**: `akaver@akaver.com` / `Foo.bar1` (admin role, FirstName=Andres, LastName=Käver)
- **JWT Config**: Key=`kadfj,hvd_fkw3...`, Issuer/Audience=`taltech.akaver.com`, Expiry=600s, ClockSkew=Zero
- **Helpers**: `JsonHelper.CamelCase`, `HttpClientExtensions`, `HtmlHelpers`

---

## Implementation Plan

### Phase 1: Test Infrastructure Enhancements

- [ ] **1.1 Create a shared `ApiTestBase` helper class** in `Tests/Helpers/` that encapsulates common operations: login, register, obtaining JWT tokens, creating authenticated `HttpRequestMessage` instances, and deserializing API responses. This eliminates duplication across all test classes. The helper should accept the `HttpClient` and expose methods like `LoginAsync(email, password, expiresInSeconds?)`, `RegisterAsync(email, password, firstName, lastName, expiresInSeconds?)`, and `CreateAuthenticatedRequest(method, url, token)`.

  **Rationale**: Every API test class currently duplicates the login/token-extraction flow (see `Tests/Integration/api/TodoCategoriesControllerTest.cs:40-53` and `Tests/Integration/api/TokenFlow.cs:33-49`). A shared helper reduces boilerplate and ensures consistent token handling.

- [ ] **1.2 Create a `TestDataFactory` helper class** in `Tests/Helpers/` that provides factory methods for creating test DTOs: `CreateTodoCategory(name?, sort?)`, `CreateTodoPriority(name?, sort?)`, `CreateTodoTask(name?, categoryId, priorityId)`, `CreateListItem(description?, completed?)`. Use sensible defaults with optional overrides.

  **Rationale**: CRUD tests need to create entities via the API before testing GET/PUT/DELETE operations. Centralizing DTO construction prevents scattered magic values and makes tests self-documenting.

- [ ] **1.3 Enhance `DataSeeder` to seed an `ApiKey` record** for the `ListItemsController` integration tests. Currently `AppDataInit.SeedAppData()` at `App.DAL.EF/Seeding/AppDataInit.cs:10-12` is empty. Add seeding logic that creates at least one `ApiKey` associated with the seeded admin user, with a known `SecretKey` GUID constant for test usage.

  **Rationale**: The `ListItemsController` requires a valid `apiKey` query parameter (`ListItemsController.cs:30`). Without a seeded API key, no ListItems integration tests can run against the test database.

### Phase 2: Identity / Account Controller Tests

#### 2A. Login Endpoint Tests (`POST /api/v1.0/Account/Login`)

- [ ] **2.1 Test successful login returns valid JWT and refresh token.** POST valid credentials, assert 200, deserialize `JwtResponse`, verify `Token` is non-empty, `RefreshToken` is a valid 36-char GUID string, `FirstName` = "Andres", `LastName` = "Käver".

  **Rationale**: Establishes the baseline happy path. Validates the full response shape from `AccountController.cs:113-121`.

- [ ] **2.2 Test login with wrong password returns 404 with error message.** POST valid email with incorrect password, assert 404 status, deserialize `Message` DTO, verify `Messages` contains "User/Password problem!".

  **Rationale**: Validates the password-failure branch at `AccountController.cs:70-76`. Ensures the API does not leak whether the email exists.

- [ ] **2.3 Test login with non-existent email returns 404 with error message.** POST a completely unknown email, assert 404, verify same generic error message "User/Password problem!".

  **Rationale**: Validates the user-not-found branch at `AccountController.cs:63-68`. Confirms timing-attack mitigation via `Task.Delay`.

- [ ] **2.4 Test JWT token content after login.** Decode the returned JWT using `JwtSecurityTokenHandler.ReadJwtToken()`, verify: (a) `ClaimTypes.Email` claim matches login email, (b) `ClaimTypes.GivenName` claim is NOT present on the seeded user since claims are only added during Register (see `AccountController.cs:171-172`), (c) `iss` = "taltech.akaver.com", (d) `aud` = "taltech.akaver.com", (e) token expiration is approximately correct based on `expiresInSeconds` parameter.

  **Rationale**: Validates JWT structure and content. The seeded user was created via `UserManager.CreateAsync()` without explicit `AddClaimAsync()` calls (see `AppDataInit.cs:61`), so the claims profile differs from Register. This catches regressions in claim generation.

- [ ] **2.5 Test login with custom `expiresInSeconds` parameter.** Login with `expiresInSeconds=3`, decode the JWT, verify the `exp` claim is approximately 3 seconds from current UTC time (within 2-second tolerance).

  **Rationale**: Validates the expiration override logic at `AccountController.cs:55-60`. The constraint that `expiresInSeconds` must be less than the configured 600 seconds is also indirectly tested.

- [ ] **2.6 Test that login cleans up expired refresh tokens.** Perform two logins for the same user (creating 2 refresh tokens), then set a short expiration on the first, wait for it to expire, login again, and verify through a subsequent refresh attempt that old tokens are invalidated.

  **Rationale**: Validates the expired-token cleanup loop at `AccountController.cs:98-105`. Prevents unbounded refresh token accumulation in the database.

#### 2B. Register Endpoint Tests (`POST /api/v1.0/Account/Register`)

- [ ] **2.7 Test successful registration returns JWT and refresh token.** Register a new user with a unique email, assert 200, deserialize `JwtResponse`, verify all fields populated, verify JWT contains `ClaimTypes.GivenName` and `ClaimTypes.Surname` claims matching the registration input.

  **Rationale**: Validates the registration happy path. Unlike login, Register explicitly adds name claims at `AccountController.cs:171-172`, so the JWT content differs.

- [ ] **2.8 Test registration with duplicate email returns 400.** Register a user, then try to register the same email again, assert 400 with message "User already registered".

  **Rationale**: Validates the duplicate-check at `AccountController.cs:141-146`.

- [ ] **2.9 Test registration with weak password returns 400 with validation errors.** Register with password "123", assert 400, verify `Message.Messages` contains Identity password validation errors.

  **Rationale**: Validates the error propagation path at `AccountController.cs:198-199`.

- [ ] **2.10 Test that newly registered user can login with the registered credentials.** Register a new user, then login with those same credentials, verify success.

  **Rationale**: End-to-end validation that the user is persisted correctly and password hashing works.

- [ ] **2.11 Test JWT content after registration contains no duplicate claims.** Register a new user, decode the JWT, enumerate all claims, assert that no claim type appears more than once (specifically check `ClaimTypes.Email`, `ClaimTypes.NameIdentifier`, `ClaimTypes.GivenName`, `ClaimTypes.Surname`).

  **Rationale**: Registration calls `AddClaimAsync()` for GivenName and Surname at `AccountController.cs:171-172`, then creates a principal via `CreateUserPrincipalAsync()` which may include those same claims from the Identity store. This test catches duplicate claim injection if `CreateUserPrincipalAsync` re-adds claims that were just stored.

#### 2C. Refresh Token Endpoint Tests (`POST /api/v1.0/Account/RefreshToken`)

- [ ] **2.12 Test single refresh cycle with valid tokens.** Login (expiresInSeconds=1), wait for JWT to expire, call RefreshToken with expired JWT + valid refresh token, assert 200, verify new JWT and new refresh token are returned, verify new refresh token differs from original.

  **Rationale**: Core refresh flow validation. This is the existing test pattern from `TokenFlow.cs:31-72` but with explicit new-vs-old token comparison.

- [ ] **2.13 Test multi-cycle refresh (3 consecutive cycles).** Login, then perform 3 sequential refresh cycles (each with expiresInSeconds=1 and a 1.5s wait). After each cycle: (a) verify new JWT is valid, (b) verify new refresh token differs from previous, (c) verify the old refresh token can no longer be used for a 4th refresh (previous token grace period expired). Track all tokens across cycles and ensure no token repetition.

  **Rationale**: This is the core requirement. Multi-cycle testing validates that the token rotation at `AccountController.cs:294-303` correctly chains: current token becomes `PreviousToken`, and a new token is issued each time. Also validates that the 1-minute `PreviousTokenExpirationDateTime` grace window (line 298) eventually expires.

- [ ] **2.14 Test refresh with PreviousToken within grace period.** Login, refresh once to rotate the token, then immediately (within 1 minute) use the OLD refresh token again. Assert 200 success since the previous token has a 1-minute grace window per `AccountController.cs:259-260`.

  **Rationale**: Validates the PreviousToken lookup branch in the LINQ query at `AccountController.cs:259`. This grace period handles race conditions where a client might retry with the old token.

- [ ] **2.15 Test refresh with completely invalid refresh token returns error.** Login, then call RefreshToken with the valid JWT but a random GUID as refresh token. Assert failure (500 status with ProblemDetails "no valid refresh tokens found").

  **Rationale**: Validates the empty-collection check at `AccountController.cs:269-272`.

- [ ] **2.16 Test refresh with malformed JWT returns 400.** Call RefreshToken with `Jwt = "not-a-jwt"` and a random refresh token. Assert 400 with message containing "Cant parse the token".

  **Rationale**: Validates the try/catch block at `AccountController.cs:223-234`.

- [ ] **2.17 Test refresh with JWT missing email claim returns 400.** Craft a minimal JWT without an email claim (use `IdentityExtensions.GenerateJwt` with an empty claims list), attempt refresh. Assert 400 with "No email in jwt".

  **Rationale**: Validates the email extraction check at `AccountController.cs:239-243`.

- [ ] **2.18 Test JWT content consistency across refresh cycles.** Login, refresh, compare claims between the original JWT and the refreshed JWT. Verify: (a) `sub` / NameIdentifier claim is identical, (b) email claim is identical, (c) issuer and audience are preserved, (d) only `exp`, `nbf`, `iat` timestamps change. No duplicate claims should appear.

  **Rationale**: Ensures that `CreateUserPrincipalAsync()` at `AccountController.cs:282` produces identical claims on each refresh. Regression guard against claim accumulation.

- [ ] **2.19 Test that refresh token DB records do not accumulate (no double records).** Register a new user, perform 5 refresh cycles. After each cycle, verify through a subsequent refresh that exactly 1 valid refresh token record exists for the user (the controller asserts `Count != 1` as an error at `AccountController.cs:274-277`). The "More than one valid refresh token found" error should never be triggered.

  **Rationale**: Directly tests the "no double records" requirement. The controller explicitly rejects `Count > 1` as a server error. This test verifies the rotation logic properly invalidates old tokens so only one valid token exists at any time.

- [ ] **2.20 Test two concurrent sessions for the same user.** Login twice (creating 2 refresh tokens), refresh using the first session's token, then refresh using the second session's token. Both should succeed independently since they are separate `RefreshToken` DB records.

  **Rationale**: Validates that multiple simultaneous sessions (each with their own refresh token) don't interfere. Each login creates a new `RefreshToken` entity at `AccountController.cs:107-109`.

### Phase 3: TodoCategories Controller Tests

- [ ] **3.1 Test GET /api/v1.0/TodoCategories without auth returns 401.** (Already exists in `TodoCategoriesControllerTest.IndexRequiresLogin` but should be part of the comprehensive suite.)

  **Rationale**: Validates the `[Authorize]` attribute on the controller at `TodoCategoriesController.cs:15`.

- [ ] **3.2 Test full CRUD lifecycle for TodoCategories.** In a single test method: (a) POST a new category, assert 201 and capture the returned ID, (b) GET the category by ID, assert 200 and verify all fields match, (c) PUT an update changing `CategoryName` and `CategorySort`, assert 200 and verify returned DTO reflects changes, (d) DELETE the category, assert 204, (e) GET the deleted category, assert 404.

  **Rationale**: End-to-end CRUD validation. Tests POST at `TodoCategoriesController.cs:110-118`, GET by ID at `52-73`, PUT at `82-101`, DELETE at `125-138`. Using a single test ensures proper data flow and avoids test ordering dependencies.

- [ ] **3.3 Test GET /api/v1.0/TodoCategories returns only current user's categories.** Register two different users, create a category for each, then GET the list for each user and verify each only sees their own category.

  **Rationale**: Validates the `AppUserId == User.GetUserId()` filter at `TodoCategoriesController.cs:33`. Ensures data isolation between users.

- [ ] **3.4 Test PUT with mismatched ID returns 400.** Send a PUT request where the URL ID differs from the body's `Id` field. Assert 400.

  **Rationale**: Validates the ID mismatch guard at `TodoCategoriesController.cs:85-88`.

- [ ] **3.5 Test DELETE of another user's category returns 404.** User A creates a category, User B tries to delete it. Assert 404 (not 403 or 200).

  **Rationale**: Validates that the ownership filter in DELETE at `TodoCategoriesController.cs:128` treats unauthorized access as "not found" rather than leaking existence.

- [ ] **3.6 Test GET list returns categories sorted by `CategorySort` then `CategoryName`.** Create 3 categories with specific sort values and names, GET the list, verify ordering matches the `OrderBy` at `TodoCategoriesController.cs:34-35`.

  **Rationale**: Validates the ordering logic that the JavaScript frontend depends on.

### Phase 4: TodoPriorities Controller Tests

- [ ] **4.1 Test GET /api/v1.0/TodoPriorities without auth returns 401.**

  **Rationale**: Validates JWT requirement at `TodoPrioritiesController.cs:15`.

- [ ] **4.2 Test full CRUD lifecycle for TodoPriorities.** POST a new priority (assert 201), GET by ID (assert 200, verify fields), PUT an update (assert 200), DELETE (assert 204), GET deleted (assert 404).

  **Rationale**: Covers all 5 endpoints. The PUT endpoint has a known IDOR TODO at `TodoPrioritiesController.cs:66` -- this test establishes baseline behavior.

- [ ] **4.3 Test GET list returns only current user's priorities and is sorted by `PrioritySort` then `PriorityName`.**

  **Rationale**: Validates the ownership filter at `TodoPrioritiesController.cs:31` and ordering at lines 32-33.

- [ ] **4.4 Test user isolation: one user cannot see/edit/delete another user's priorities.**

  **Rationale**: Cross-user security validation. Note the PUT endpoint IDOR concern at `TodoPrioritiesController.cs:66` -- this test should document the current (potentially insecure) behavior.

### Phase 5: TodoTasks Controller Tests

- [ ] **5.1 Test GET /api/v1.0/TodoTasks without auth returns 401.**

  **Rationale**: Validates JWT requirement at `TodoTasksController.cs:15`.

- [ ] **5.2 Test full CRUD lifecycle for TodoTasks.** First create a category and priority (prerequisites), then: POST a new task referencing them (assert 201), GET by ID (assert 200, verify all fields including `TodoCategoryId` and `TodoPriorityId`), PUT an update (assert 200), DELETE (assert 204), GET deleted (assert 404).

  **Rationale**: TodoTasks depend on existing categories and priorities (validated at `TodoTasksController.cs:76-83` and `105-108`). The test must create prerequisites first.

- [ ] **5.3 Test POST TodoTask with non-existent CategoryId returns 400.** Provide a random GUID as `TodoCategoryId`. Assert 400 with "Could not find category or priority for current user!".

  **Rationale**: Validates the FK existence check at `TodoTasksController.cs:105-108`.

- [ ] **5.4 Test POST TodoTask with another user's CategoryId returns 400.** User A creates a category, User B tries to create a task using that category ID. Assert 400.

  **Rationale**: Validates the ownership check `x.AppUserId == User.GetUserId()` at `TodoTasksController.cs:106`.

- [ ] **5.5 Test GET list returns tasks sorted by `TaskSort` then `CreatedDt`.**

  **Rationale**: Validates ordering at `TodoTasksController.cs:35-36`.

- [ ] **5.6 Test user isolation: one user cannot see/edit/delete another user's tasks.**

  **Rationale**: TodoTask ownership is derived through the Category and Priority ownership (see the double AppUserId check at `TodoTasksController.cs:32-33`). This indirect ownership model needs validation.

### Phase 6: ListItems Controller Tests (API Key Auth)

- [ ] **6.1 Test GET /api/v1.0/ListItems without apiKey returns 400.** Call without the `apiKey` query parameter. Assert 400 with "Problem with ApiKey".

  **Rationale**: Validates the API key check at `ListItemsController.cs:30-33`.

- [ ] **6.2 Test GET /api/v1.0/ListItems with invalid apiKey returns 400.**

  **Rationale**: Validates `IsApiKeyValid()` at `ListItemsController.cs:165-168`.

- [ ] **6.3 Test full CRUD lifecycle for ListItems.** Using the seeded API key: POST a new item (assert 201), GET by ID (assert 200), PUT an update toggling `Completed` (assert 204), GET list (assert item present), DELETE (assert 200 with returned item), GET deleted (assert 404).

  **Rationale**: Covers all 5 endpoints of the API-key-authenticated controller.

- [ ] **6.4 Test GET ListItems with `completed` filter.** Create items with mixed `Completed` states, GET with `completed=true`, verify only completed items returned. Repeat with `completed=false`.

  **Rationale**: Validates the optional filter at `ListItemsController.cs:39-42`.

- [ ] **6.5 Test that one API key cannot access another API key's items.** Create two API keys (requires seeding or MVC setup), create items under each, verify cross-key access returns 404 for GET-by-ID and no results for GET-list.

  **Rationale**: Validates the `l.ApiKey!.SecretKey == apiKey` filter throughout the controller.

- [ ] **6.6 Test PUT with mismatched ID returns 400.**

  **Rationale**: Validates the ID check at `ListItemsController.cs:79-82`.

### Phase 7: Cross-Cutting Concerns

- [ ] **7.1 Test expired JWT is rejected by authenticated endpoints.** Login with `expiresInSeconds=1`, wait 2 seconds, attempt GET TodoCategories with the expired token. Assert 401. (ClockSkew is Zero per `Program.cs:72`.)

  **Rationale**: Validates the strict token expiration behavior. The Zero ClockSkew configuration means tokens expire at exactly the stated time.

- [ ] **7.2 Test JWT from one user cannot access another user's data.** User A logs in and creates a category. User B logs in and attempts GET on User A's category by ID. Assert 404.

  **Rationale**: End-to-end authorization test spanning the JWT middleware and controller ownership filters.

- [ ] **7.3 Test API versioning with explicit version in URL.** Hit `/api/v1.0/TodoCategories` (with auth), assert 200. Optionally test `/api/v2.0/TodoCategories` returns 400 or equivalent (no v2 defined).

  **Rationale**: Validates the API versioning middleware configured at `Program.cs:131-147`.

## File Structure

All new test files should be placed under the existing `Tests/` project structure:

```
Tests/
  Helpers/
    ApiTestBase.cs              (Phase 1.1 - shared auth helper)
    TestDataFactory.cs          (Phase 1.2 - DTO factory)
    DataSeeder.cs               (Phase 1.3 - enhanced seeding)
    JsonHelper.cs               (existing)
    HttpClientExtensions.cs     (existing)
    HtmlHelpers.cs              (existing)
  Integration/
    api/
      Identity/
        AccountLoginTest.cs     (Phase 2A - tests 2.1-2.6)
        AccountRegisterTest.cs  (Phase 2B - tests 2.7-2.11)
        AccountRefreshTokenTest.cs (Phase 2C - tests 2.12-2.20)
      TodoCategoriesControllerTest.cs  (Phase 3 - tests 3.1-3.6, extends existing)
      TodoPrioritiesControllerTest.cs  (Phase 4 - tests 4.1-4.4)
      TodoTasksControllerTest.cs       (Phase 5 - tests 5.1-5.6)
      ListItemsControllerTest.cs       (Phase 6 - tests 6.1-6.6)
      CrossCuttingTest.cs              (Phase 7 - tests 7.1-7.3)
    CustomPostgresWebApplicationFactory.cs (existing)
```

## Verification Criteria

- All 43 new test methods compile and pass against the `WebApp_TESTING` PostgreSQL database
- Every API controller action (23 total endpoints) is covered by at least one test
- JWT refresh cycle is validated across 3+ consecutive cycles with token uniqueness assertions
- JWT claim content is verified for both Login and Register paths, with explicit no-duplicate-claims assertions
- Refresh token DB records are validated to not accumulate (max 1 valid record per session)
- User data isolation is confirmed for all 4 data controllers (TodoCategories, TodoPriorities, TodoTasks, ListItems)
- All tests use the `[Collection("NonParallel")]` attribute to match the existing convention and prevent concurrent DB access
- No test depends on execution order; each test sets up its own prerequisites via API calls

## Potential Risks and Mitigations

1. **Test execution time due to `Task.Delay` in refresh token tests.**
   Mitigation: Use `expiresInSeconds=1` with 1.5-second waits (matching existing `TokenFlow.cs` pattern). Multi-cycle tests (2.13, 2.19) will take ~4.5-7.5 seconds each. This is acceptable for integration tests.

2. **Database state pollution between tests within the same class.**
   Mitigation: Each test should create its own users/data via Register or Login, using unique email addresses (e.g., `$"testuser-{Guid.NewGuid():N}@test.com"`). The factory drops/recreates the database per `IClassFixture` lifetime, not per test method, so tests within a class share state.

3. **Race conditions in `CustomWebApplicationFactory` database setup.**
   Mitigation: All tests are already serialized via `[Collection("NonParallel")]`. Maintain this convention for all new test classes.

4. **ListItems tests require seeded ApiKey data.**
   Mitigation: Phase 1.3 explicitly addresses this. The seeded `ApiKey.SecretKey` GUID must be a known constant accessible from test code, stored as a public constant in `InitialData` or a test-specific constants file.

5. **The PreviousToken grace period test (2.14) depends on timing.**
   Mitigation: Immediately attempt the old-token refresh after rotation (well within the 1-minute grace window). No `Task.Delay` needed for this test.

6. **Register tests create users that persist across tests within the class fixture lifetime.**
   Mitigation: Use unique, random email addresses for each test to avoid `"User already registered"` conflicts. Never reuse the seeded admin email for registration tests.

7. **TodoPriorities PUT endpoint has IDOR vulnerability** (TODO at `TodoPrioritiesController.cs:66`).
   Mitigation: Document the current behavior in the test. The test should verify what actually happens (PUT succeeds even for another user's priority ID, overwriting ownership), establishing a regression baseline. Add a comment noting this is a known security concern.

## Alternative Approaches

1. **In-memory SQLite instead of PostgreSQL for tests**: The project already has `Microsoft.EntityFrameworkCore.InMemory` referenced in `Tests.csproj:25`. Using an in-memory provider would eliminate the PostgreSQL dependency and speed up tests. **Trade-off**: PostgreSQL-specific features (JSONB, Npgsql type mapping) wouldn't be tested. The existing factory deliberately uses real PostgreSQL for this reason. Recommendation: keep PostgreSQL for integration tests.

2. **Unit tests with mocked DbContext via NSubstitute**: The project references NSubstitute (`Tests.csproj:26`). Controller methods could be unit-tested by mocking `AppDbContext`, `UserManager`, and `SignInManager`. **Trade-off**: Higher test count but less realistic; won't catch EF query translation issues, middleware pipeline problems, or authentication configuration bugs. Recommendation: prioritize integration tests for API controllers; reserve unit tests for `IdentityExtensions.GenerateJwt()` and DTO mapping methods where the logic is pure and self-contained.

3. **Shared fixture vs per-test database reset**: The current approach resets the database once per `IClassFixture` lifetime. An alternative is to use `IAsyncLifetime` per test class to reset between tests. **Trade-off**: Slower execution but guaranteed isolation. Recommendation: use unique data per test (random emails, new entities) within the shared fixture, matching the existing pattern.
