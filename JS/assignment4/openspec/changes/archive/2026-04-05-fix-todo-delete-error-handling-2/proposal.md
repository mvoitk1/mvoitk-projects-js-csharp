## Why

The MVC controllers for TodoCategories and TodoPriorities have no error handling on their `DeleteConfirmed` actions. When a user tries to delete a category or priority that has dependent TodoTask records, `SaveChangesAsync()` throws an unhandled `DbUpdateException` due to the global `DeleteBehavior.Restrict` configuration, resulting in a 500 error page with no explanation. The API controllers already handle this correctly (returning 409 Conflict with dependent count), but the MVC side was not addressed.

## What Changes

- Add pre-delete dependency checks in `TodoCategoriesController.DeleteConfirmed` and `TodoPrioritiesController.DeleteConfirmed` to count dependent TodoTask records before attempting deletion
- Add `DbUpdateException` catch as a safety net in both controllers' `DeleteConfirmed` actions
- Pass error messages and dependent record counts to the Delete views via `ViewData`
- Update `TodoCategories/Delete.cshtml` and `TodoPriorities/Delete.cshtml` to display a Bootstrap alert with the error message when deletion fails, showing the number of dependent tasks
- Show dependent task count proactively on the Delete confirmation page (GET action) so the user is warned before attempting deletion

## Capabilities

### New Capabilities

- `mvc-delete-error-handling`: MVC controller and view error handling for delete operations when entities have dependent records, including user-facing error messages with dependent record counts

### Modified Capabilities

_(none — the existing `delete-error-handling` spec covers API endpoints only; MVC is a separate capability)_

## Impact

- **Controllers:** `WebApp/Controllers/TodoCategoriesController.cs`, `WebApp/Controllers/TodoPrioritiesController.cs` — both `Delete` (GET) and `DeleteConfirmed` (POST) actions modified
- **Views:** `WebApp/Views/TodoCategories/Delete.cshtml`, `WebApp/Views/TodoPriorities/Delete.cshtml` — updated to display error alerts and dependency warnings
- **No breaking changes** — existing behavior for entities without dependents is unchanged
