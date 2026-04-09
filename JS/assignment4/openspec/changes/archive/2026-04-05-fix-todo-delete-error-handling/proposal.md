## Why

The `DeleteTodoPriority` and `DeleteTodoCategory` API endpoints throw unhandled `DbUpdateException` (500 Internal Server Error) when attempting to delete a priority or category that has dependent `TodoTask` records. Cascade delete is globally disabled (`DeleteBehavior.Restrict` in `BaseDbContext`), so the database rejects the delete with a foreign key constraint violation. The endpoints need proper error handling to return a meaningful HTTP response instead of a 500.

## What Changes

- Add try-catch error handling around `SaveChangesAsync()` in `DeleteTodoPriority` endpoint to catch `DbUpdateException` caused by FK constraint violations
- Add try-catch error handling around `SaveChangesAsync()` in `DeleteTodoCategory` endpoint to catch `DbUpdateException` caused by FK constraint violations
- Return appropriate HTTP error response (409 Conflict) with a descriptive message when deletion fails due to dependent entities
- Add integration tests for the cascade delete failure scenarios

## Capabilities

### New Capabilities
- `delete-error-handling`: Proper error handling for delete operations on entities with dependent records, returning 409 Conflict instead of 500 Internal Server Error

### Modified Capabilities

## Impact

- **API Controllers**: `TodoPrioritiesController.cs` and `TodoCategoriesController.cs` delete endpoints modified
- **API Contract**: New 409 Conflict response for delete endpoints (previously returned 500)
- **Tests**: New integration tests covering delete-with-dependents scenarios
