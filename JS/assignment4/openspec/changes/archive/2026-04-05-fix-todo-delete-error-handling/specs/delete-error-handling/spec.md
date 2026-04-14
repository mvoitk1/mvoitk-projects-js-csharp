## ADDED Requirements

### Requirement: Delete endpoint returns 409 Conflict when entity has dependent records

The `DeleteTodoPriority` and `DeleteTodoCategory` API endpoints SHALL return HTTP 409 Conflict when the delete operation fails due to dependent `TodoTask` records referencing the entity being deleted. The response body SHALL contain a descriptive error message indicating that the entity cannot be deleted because it has dependent records, and SHALL include the count of dependent `TodoTask` records associated with the entity being deleted.

#### Scenario: Delete TodoPriority with dependent TodoTasks
- **WHEN** a DELETE request is sent to `/api/TodoPriorities/{id}` and the priority has one or more associated TodoTask records
- **THEN** the API returns HTTP 409 Conflict with a response body containing an error message and the count of dependent TodoTask records

#### Scenario: Delete TodoCategory with dependent TodoTasks
- **WHEN** a DELETE request is sent to `/api/TodoCategories/{id}` and the category has one or more associated TodoTask records
- **THEN** the API returns HTTP 409 Conflict with a response body containing an error message and the count of dependent TodoTask records

#### Scenario: Delete TodoPriority with no dependent TodoTasks still succeeds
- **WHEN** a DELETE request is sent to `/api/TodoPriorities/{id}` and the priority has no associated TodoTask records
- **THEN** the API returns HTTP 204 No Content and the priority is deleted

#### Scenario: Delete TodoCategory with no dependent TodoTasks still succeeds
- **WHEN** a DELETE request is sent to `/api/TodoCategories/{id}` and the category has no associated TodoTask records
- **THEN** the API returns HTTP 204 No Content and the category is deleted
