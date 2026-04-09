## ADDED Requirements

### Requirement: ApiTestBase static helper class

A static helper class in `Tests/Helpers/ApiTestBase.cs` that encapsulates common API test operations, accepting `HttpClient` as a parameter.

#### Scenario: LoginAsync returns JwtResponse
- **WHEN** `ApiTestBase.LoginAsync(client, email, password, expiresInSeconds?)` is called with valid credentials
- **THEN** it POSTs to `/api/v1.0/Account/Login`, deserializes the response to `JwtResponse`, and returns it
- **THEN** it calls `EnsureSuccessStatusCode()` on the response

#### Scenario: RegisterAsync returns JwtResponse
- **WHEN** `ApiTestBase.RegisterAsync(client, email, password, firstName, lastName, expiresInSeconds?)` is called
- **THEN** it POSTs to `/api/v1.0/Account/Register`, deserializes the response to `JwtResponse`, and returns it
- **THEN** it calls `EnsureSuccessStatusCode()` on the response

#### Scenario: CreateAuthMessage builds authenticated HttpRequestMessage
- **WHEN** `ApiTestBase.CreateAuthMessage(method, url, token)` is called
- **THEN** it returns an `HttpRequestMessage` with `Authorization: Bearer {token}` header and `Accept: application/json` header

### Requirement: TestDataFactory static helper class

A static helper class in `Tests/Helpers/TestDataFactory.cs` that provides factory methods for creating test DTOs with sensible defaults and unique names.

#### Scenario: CreateTodoCategory with defaults
- **WHEN** `TestDataFactory.CreateTodoCategory()` is called without arguments
- **THEN** it returns a `TodoCategoryCreate` with a unique `CategoryName` containing a GUID fragment and `CategorySort = 0`

#### Scenario: CreateTodoCategory with overrides
- **WHEN** `TestDataFactory.CreateTodoCategory(name: "Custom", sort: 5)` is called
- **THEN** it returns a `TodoCategoryCreate` with `CategoryName = "Custom"` and `CategorySort = 5`

#### Scenario: CreateTodoPriority with defaults
- **WHEN** `TestDataFactory.CreateTodoPriority()` is called without arguments
- **THEN** it returns a `TodoPriorityCreate` with a unique `PriorityName` and `PrioritySort = 0`

#### Scenario: CreateTodoTask with required parameters
- **WHEN** `TestDataFactory.CreateTodoTask(categoryId, priorityId)` is called
- **THEN** it returns a `TodoTaskCreate` with a unique `TaskName`, the provided FK IDs, `IsCompleted = false`, `IsArchived = false`

#### Scenario: CreateListItem with defaults
- **WHEN** `TestDataFactory.CreateListItem()` is called
- **THEN** it returns an anonymous object with a unique `Description` and `Completed = false`, suitable for JSON serialization to `ListItem`

### Requirement: ApiKey data seeding for integration tests

Enhance `AppDataInit.SeedAppData()` and `InitialData` to seed at least one `ApiKey` record with a known constant `SecretKey` GUID associated with the seeded admin user.

#### Scenario: Seeded ApiKey available in test database
- **WHEN** the `CustomWebApplicationFactory` seeds the test database
- **THEN** the `ApiKeys` table contains a record with `SecretKey` matching the well-known constant (e.g., `Guid.Parse("00000000-0000-0000-0000-000000000001")`)
- **THEN** the seeded `ApiKey.AppUserId` matches the seeded admin user's ID
- **THEN** the seeded `ApiKey.IsDisabled` is `false`

#### Scenario: Second ApiKey seeded for cross-key isolation tests
- **WHEN** the `CustomWebApplicationFactory` seeds the test database
- **THEN** the `ApiKeys` table contains a second record with a different known `SecretKey` constant (e.g., `Guid.Parse("00000000-0000-0000-0000-000000000002")`)
- **THEN** this enables cross-key isolation tests in ListItems without requiring MVC-based key creation

### Requirement: All test classes use NonParallel collection

#### Scenario: Test serialization
- **WHEN** any new test class is created
- **THEN** it must be decorated with `[Collection("NonParallel")]` to match the existing convention and prevent concurrent database access
