## ADDED Requirements

### Requirement: TodoCategories requires JWT authentication

#### Scenario: Unauthenticated request returns 401
- **WHEN** GET `/api/v1.0/TodoCategories` without an Authorization header
- **THEN** response status is 401 Unauthorized

### Requirement: TodoCategories full CRUD lifecycle

Verify that a category can be created, read, updated, and deleted through the API.

#### Scenario: Create, read, update, delete a category
- **WHEN** POST `/api/v1.0/TodoCategories` with `{"categoryName": "Test", "categorySort": 1}` using a valid JWT
- **THEN** response status is 201 and response body contains the created category with a generated `Id`
- **WHEN** GET `/api/v1.0/TodoCategories/{id}` with that `Id`
- **THEN** response status is 200 and all fields match the created values
- **WHEN** PUT `/api/v1.0/TodoCategories/{id}` with updated `categoryName` and `categorySort`
- **THEN** response status is 200 and response body reflects the updated values
- **WHEN** DELETE `/api/v1.0/TodoCategories/{id}`
- **THEN** response status is 204
- **WHEN** GET `/api/v1.0/TodoCategories/{id}` again
- **THEN** response status is 404

### Requirement: TodoCategories user data isolation

Each user must only see their own categories. A user must not be able to read another user's categories.

#### Scenario: Two users see only their own categories
- **WHEN** User A registers and creates a category "CatA"
- **WHEN** User B registers and creates a category "CatB"
- **WHEN** User A GET `/api/v1.0/TodoCategories`
- **THEN** response contains only "CatA", not "CatB"
- **WHEN** User B GET `/api/v1.0/TodoCategories`
- **THEN** response contains only "CatB", not "CatA"

### Requirement: TodoCategories PUT rejects mismatched ID

#### Scenario: URL ID differs from body ID
- **WHEN** PUT `/api/v1.0/TodoCategories/{idA}` with a body containing `Id = {idB}` where `idA != idB`
- **THEN** response status is 400

### Requirement: TodoCategories DELETE of another user's category returns 404

Ownership is enforced via the `AppUserId` filter. Attempting to delete a category owned by another user should appear as "not found", not "forbidden".

#### Scenario: Cross-user delete attempt
- **WHEN** User A creates a category and obtains its `Id`
- **WHEN** User B attempts DELETE `/api/v1.0/TodoCategories/{id}` using their own JWT
- **THEN** response status is 404

### Requirement: TodoCategories list is sorted by CategorySort then CategoryName

#### Scenario: Verify sort order
- **WHEN** a user creates 3 categories: (name="Zebra", sort=1), (name="Alpha", sort=2), (name="Beta", sort=1)
- **WHEN** GET `/api/v1.0/TodoCategories`
- **THEN** response contains categories in order: (sort=1, name="Beta"), (sort=1, name="Zebra"), (sort=2, name="Alpha")

---

### Requirement: TodoPriorities requires JWT authentication

#### Scenario: Unauthenticated request returns 401
- **WHEN** GET `/api/v1.0/TodoPriorities` without an Authorization header
- **THEN** response status is 401 Unauthorized

### Requirement: TodoPriorities full CRUD lifecycle

#### Scenario: Create, read, update, delete a priority
- **WHEN** POST `/api/v1.0/TodoPriorities` with `{"priorityName": "High", "prioritySort": 1}` using a valid JWT
- **THEN** response status is 201 and response body contains the created priority with a generated `Id`
- **WHEN** GET `/api/v1.0/TodoPriorities/{id}` with that `Id`
- **THEN** response status is 200 and all fields match the created values
- **WHEN** PUT `/api/v1.0/TodoPriorities/{id}` with updated `priorityName` and `prioritySort`
- **THEN** response status is 200
- **WHEN** DELETE `/api/v1.0/TodoPriorities/{id}`
- **THEN** response status is 204
- **WHEN** GET `/api/v1.0/TodoPriorities/{id}` again
- **THEN** response status is 404

### Requirement: TodoPriorities list is sorted and filtered by user

#### Scenario: User sees only their own priorities sorted by PrioritySort then PriorityName
- **WHEN** User A creates priorities: (name="Low", sort=2), (name="High", sort=1), (name="Critical", sort=1)
- **WHEN** User A GET `/api/v1.0/TodoPriorities`
- **THEN** response contains priorities in order: (sort=1, name="Critical"), (sort=1, name="High"), (sort=2, name="Low")
- **WHEN** User B registers and GET `/api/v1.0/TodoPriorities`
- **THEN** response is an empty list (User B has no priorities)

### Requirement: TodoPriorities user isolation for read/edit/delete

Note: The PUT endpoint has a known IDOR issue where it uses `EntityState.Modified` without verifying the existing record belongs to the current user. This test documents current behavior.

#### Scenario: Cross-user access attempts
- **WHEN** User A creates a priority and obtains its `Id`
- **WHEN** User B attempts GET `/api/v1.0/TodoPriorities/{id}`
- **THEN** response status is 404
- **WHEN** User B attempts DELETE `/api/v1.0/TodoPriorities/{id}`
- **THEN** response status is 404

---

### Requirement: TodoTasks requires JWT authentication

#### Scenario: Unauthenticated request returns 401
- **WHEN** GET `/api/v1.0/TodoTasks` without an Authorization header
- **THEN** response status is 401 Unauthorized

### Requirement: TodoTasks full CRUD lifecycle

TodoTasks require existing TodoCategory and TodoPriority records as foreign keys.

#### Scenario: Create prerequisites then CRUD a task
- **WHEN** a user creates a TodoCategory and a TodoPriority via their respective POST endpoints
- **WHEN** POST `/api/v1.0/TodoTasks` with a body referencing the created `TodoCategoryId` and `TodoPriorityId`
- **THEN** response status is 201 and response body contains the created task with correct field values
- **WHEN** GET `/api/v1.0/TodoTasks/{id}`
- **THEN** response status is 200 and all fields match, including `TodoCategoryId` and `TodoPriorityId`
- **WHEN** PUT `/api/v1.0/TodoTasks/{id}` with updated `TaskName` and `IsCompleted`
- **THEN** response status is 200
- **WHEN** DELETE `/api/v1.0/TodoTasks/{id}`
- **THEN** response status is 204
- **WHEN** GET `/api/v1.0/TodoTasks/{id}` again
- **THEN** response status is 404

### Requirement: TodoTasks POST rejects non-existent foreign keys

#### Scenario: Create task with random GUID as CategoryId
- **WHEN** POST `/api/v1.0/TodoTasks` with a valid `TodoPriorityId` but a random GUID as `TodoCategoryId`
- **THEN** response status is 400
- **THEN** response body contains "Could not find category or priority for current user!"

### Requirement: TodoTasks POST rejects another user's foreign keys

#### Scenario: Create task using another user's category
- **WHEN** User A creates a TodoCategory and a TodoPriority
- **WHEN** User B registers and creates their own TodoPriority
- **WHEN** User B attempts POST `/api/v1.0/TodoTasks` referencing User A's `TodoCategoryId` and User B's `TodoPriorityId`
- **THEN** response status is 400

### Requirement: TodoTasks list is sorted by TaskSort then CreatedDt

#### Scenario: Verify sort order
- **WHEN** a user creates a category, a priority, and 3 tasks with varying `TaskSort` values
- **WHEN** GET `/api/v1.0/TodoTasks`
- **THEN** response contains tasks ordered by `TaskSort` ascending, then `CreatedDt` ascending

### Requirement: TodoTasks user isolation

TodoTask ownership is derived indirectly through the Category and Priority ownership (`TodoCategory.AppUserId` and `TodoPriority.AppUserId`).

#### Scenario: Cross-user task access
- **WHEN** User A creates a category, priority, and task
- **WHEN** User B registers and GET `/api/v1.0/TodoTasks`
- **THEN** User B's response is an empty list
- **WHEN** User B attempts GET `/api/v1.0/TodoTasks/{userA_taskId}`
- **THEN** response status is 404
- **WHEN** User B attempts DELETE `/api/v1.0/TodoTasks/{userA_taskId}`
- **THEN** response status is 404
