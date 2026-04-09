## Why

The project has 23 API endpoints across 5 controllers but only 6 test methods covering a small fraction of them. There are zero tests for TodoPriorities, TodoTasks, and ListItems controllers. The JWT identity flow has only a single-cycle refresh test with no validation of token content, no duplicate-claim detection, and no multi-cycle rotation verification. This gap leaves authentication bugs, data isolation failures, and token accumulation issues undetectable until production.

## What Changes

- Add shared test infrastructure: `ApiTestBase` helper for login/register/token management, `TestDataFactory` for DTO construction, and enhanced `DataSeeder` with ApiKey seeding for ListItems tests
- Add 6 integration tests for `AccountController` Login endpoint: happy path, wrong password, non-existent user, JWT content validation, custom expiration, expired token cleanup
- Add 5 integration tests for `AccountController` Register endpoint: happy path with claim verification, duplicate email, weak password, register-then-login round-trip, no-duplicate-claims assertion
- Add 9 integration tests for `AccountController` RefreshToken endpoint: single cycle, **multi-cycle (3+ consecutive rotations)**, PreviousToken grace period, invalid refresh token, malformed JWT, missing email claim, **JWT content consistency across cycles**, **no-double-records DB verification**, concurrent sessions
- Add 6 integration tests for `TodoCategoriesController`: auth guard, full CRUD lifecycle, user isolation, ID mismatch, cross-user delete, sort order
- Add 4 integration tests for `TodoPrioritiesController`: auth guard, full CRUD lifecycle, user isolation with sort verification, cross-user access (documents IDOR behavior)
- Add 6 integration tests for `TodoTasksController`: auth guard, full CRUD lifecycle with prerequisite creation, invalid FK, cross-user FK, sort order, user isolation
- Add 6 integration tests for `ListItemsController`: missing API key, invalid API key, full CRUD lifecycle, completed filter, cross-key isolation, ID mismatch
- Add 3 cross-cutting integration tests: expired JWT rejection, cross-user data access, API versioning

## Capabilities

### New Capabilities
- `identity-api-tests`: Integration tests for AccountController covering Login, Register, and RefreshToken endpoints with JWT content validation, multi-cycle refresh verification, and duplicate record prevention
- `todo-api-tests`: Integration tests for TodoCategoriesController, TodoPrioritiesController, and TodoTasksController covering full CRUD lifecycles, user data isolation, sort ordering, and FK validation
- `listitem-api-tests`: Integration tests for ListItemsController covering API key authentication, full CRUD lifecycle, completion filtering, and cross-key data isolation
- `api-test-infrastructure`: Shared test helpers (ApiTestBase, TestDataFactory) and enhanced data seeding (ApiKey) that support all API controller test suites
- `api-cross-cutting-tests`: Integration tests for cross-cutting concerns including JWT expiration enforcement, cross-user authorization, and API versioning

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Test project (`Tests/`)**: 8 new test files, 2 new helper classes, 1 modified helper (DataSeeder). ~43 new test methods total.
- **Seeding (`App.DAL.EF/Seeding/`)**: `AppDataInit.SeedAppData()` and `InitialData` modified to seed at least one `ApiKey` record with a known `SecretKey` GUID constant for ListItems test access.
- **Test database**: `WebApp_TESTING` PostgreSQL database will have additional seeded ApiKey data. All tests use `[Collection("NonParallel")]` matching existing convention.
- **Build/CI**: Test execution time increases by ~30-60 seconds due to `Task.Delay` waits in JWT refresh cycle tests (expiresInSeconds=1 with 1.5s waits per cycle).
- **No production code changes**: All changes are confined to the test project and data seeding. No API controller logic, DTOs, domain models, or middleware are modified.
