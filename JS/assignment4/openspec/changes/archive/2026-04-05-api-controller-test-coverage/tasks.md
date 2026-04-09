## 1. Test Infrastructure: Data Seeding

- [x] 1.1 Add two known `SecretKey` GUID constants to `App.DAL.EF/Seeding/InitialData.cs` for API key test access (e.g., `ApiKey1Secret = Guid.Parse("00000000-0000-0000-0000-000000000001")`, `ApiKey2Secret = Guid.Parse("00000000-0000-0000-0000-000000000002")`)
- [x] 1.2 Implement `AppDataInit.SeedAppData()` in `App.DAL.EF/Seeding/AppDataInit.cs` to seed two `ApiKey` records associated with the seeded admin user, using the constants from 1.1, with `IsDisabled = false` and distinct `AppName` values
- [x] 1.3 Verify the seeding works by running the existing test suite (`dotnet test`) — all 6 existing tests must still pass

## 2. Test Infrastructure: Helper Classes

- [x] 2.1 Create `Tests/Helpers/ApiTestBase.cs`
- [x] 2.2 Create `Tests/Helpers/TestDataFactory.cs`

## 3. Identity Tests: Account Login

- [x] 3.1 Create `Tests/Integration/api/Identity/AccountLoginTest.cs` with `[Collection("NonParallel")]`, `IClassFixture<CustomWebApplicationFactory<Program>>`, and a constructor that creates an `HttpClient` with `AllowAutoRedirect = false`
- [x] 3.2 Implement test `LoginSuccess_ReturnsJwtResponse`
- [x] 3.3 Implement test `LoginWrongPassword_Returns404WithGenericError`
- [x] 3.4 Implement test `LoginNonExistentEmail_Returns404WithGenericError`
- [x] 3.5 Implement test `LoginJwtContent_ContainsCorrectClaims`
- [x] 3.6 Implement test `LoginCustomExpiration_JwtExpiresAtRequestedTime`
- [x] 3.7 Implement test `MultipleLogins_EachProducesDistinctRefreshToken` (renamed from `LoginCleansUpExpiredRefreshTokens` — refresh tokens have 7-day expiration, making cleanup untestable via API)

## 4. Identity Tests: Account Register

- [x] 4.1 Create `Tests/Integration/api/Identity/AccountRegisterTest.cs`
- [x] 4.2 Implement test `RegisterSuccess_ReturnsJwtWithNameClaims`
- [x] 4.3 Implement test `RegisterDuplicateEmail_Returns400`
- [x] 4.4 Implement test `RegisterWeakPassword_Returns400WithErrors`
- [x] 4.5 Implement test `RegisterThenLogin_Succeeds`
- [x] 4.6 Implement test `RegisterJwt_NoDuplicateClaims`

## 5. Identity Tests: Account RefreshToken

- [x] 5.1 Create `Tests/Integration/api/Identity/AccountRefreshTokenTest.cs`
- [x] 5.2 Implement test `SingleRefreshCycle_ReturnsNewTokens`
- [x] 5.3 Implement test `MultiCycleRefresh_ThreeConsecutiveCycles`
- [x] 5.4 Implement test `PreviousTokenGracePeriod_OldTokenAcceptedImmediately`
- [x] 5.5 Implement test `InvalidRefreshToken_ReturnsError`
- [x] 5.6 Implement test `MalformedJwt_Returns400`
- [x] 5.7 Implement test `JwtMissingEmailClaim_Returns400`
- [x] 5.8 Implement test `JwtContentConsistencyAcrossRefresh`
- [x] 5.9 Implement test `NoDoubleRecords_FiveCyclesSucceed`
- [x] 5.10 Implement test `ConcurrentSessions_IndependentRefresh`

## 6. TodoCategories Controller Tests

- [x] 6.1 Add new tests to existing `Tests/Integration/api/TodoCategoriesControllerTest.cs`
- [x] 6.2 Implement test `CrudLifecycle_CreateReadUpdateDelete`
- [x] 6.3 Implement test `UserIsolation_EachUserSeesOnlyOwnCategories`
- [x] 6.4 Implement test `PutMismatchedId_Returns400`
- [x] 6.5 Implement test `DeleteOtherUsersCategory_Returns404`
- [x] 6.6 Implement test `GetList_SortedByCategorySortThenName`

## 7. TodoPriorities Controller Tests

- [x] 7.1 Create `Tests/Integration/api/TodoPrioritiesControllerTest.cs`
- [x] 7.2 Implement test `GetWithoutAuth_Returns401`
- [x] 7.3 Implement test `CrudLifecycle_CreateReadUpdateDelete`
- [x] 7.4 Implement test `GetList_FilteredByUserAndSorted`
- [x] 7.5 Implement test `UserIsolation_CrossUserAccessDenied`

## 8. TodoTasks Controller Tests

- [x] 8.1 Create `Tests/Integration/api/TodoTasksControllerTest.cs`
- [x] 8.2 Implement test `GetWithoutAuth_Returns401`
- [x] 8.3 Implement test `CrudLifecycle_CreateReadUpdateDelete`
- [x] 8.4 Implement test `PostWithNonExistentCategoryId_Returns400`
- [x] 8.5 Implement test `PostWithOtherUsersCategoryId_Returns400`
- [x] 8.6 Implement test `GetList_SortedByTaskSortThenCreatedDt`
- [x] 8.7 Implement test `UserIsolation_CrossUserAccessDenied`

## 9. ListItems Controller Tests

- [x] 9.1 Create `Tests/Integration/api/ListItemsControllerTest.cs`
- [x] 9.2 Implement test `GetWithoutApiKey_Returns400`
- [x] 9.3 Implement test `GetWithInvalidApiKey_Returns400`
- [x] 9.4 Implement test `CrudLifecycle_CreateReadUpdateListDelete`
- [x] 9.5 Implement test `GetList_CompletedFilter`
- [x] 9.6 Implement test `CrossKeyIsolation_KeyBCannotSeeKeyAItems`
- [x] 9.7 Implement test `PutMismatchedId_Returns400`

## 10. Cross-Cutting Tests

- [x] 10.1 Create `Tests/Integration/api/CrossCuttingTest.cs`
- [x] 10.2 Implement test `ExpiredJwt_Returns401`
- [x] 10.3 Implement test `CrossUserDataAccess_Returns404`
- [x] 10.4 Implement test `ApiVersioning_V1Returns200_V2ReturnsError`

## 11. Final Verification

- [x] 11.1 Run full test suite with `dotnet test` and verify all new and existing tests pass
- [x] 11.2 Verify test count: 50 total (6 existing + 44 new) — all passing
