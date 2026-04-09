## 1. Controller Changes

- [x] 1.1 Update `TodoCategoriesController.Delete` (GET) to query dependent TodoTask count and pass it to the view via `ViewData["DependentTaskCount"]`
- [x] 1.2 Update `TodoCategoriesController.DeleteConfirmed` (POST) to pre-check dependent count, return view with error message if > 0, and wrap `SaveChangesAsync` in try/catch for `DbUpdateException`
- [x] 1.3 Update `TodoPrioritiesController.Delete` (GET) to query dependent TodoTask count and pass it to the view via `ViewData["DependentTaskCount"]`
- [x] 1.4 Update `TodoPrioritiesController.DeleteConfirmed` (POST) to pre-check dependent count, return view with error message if > 0, and wrap `SaveChangesAsync` in try/catch for `DbUpdateException`

## 2. View Changes

- [x] 2.1 Update `Views/TodoCategories/Delete.cshtml` to show a Bootstrap `alert-danger` when `ViewData["ErrorMessage"]` is set, display dependent task count, and disable the Delete button when dependencies exist
- [x] 2.2 Update `Views/TodoPriorities/Delete.cshtml` to show a Bootstrap `alert-danger` when `ViewData["ErrorMessage"]` is set, display dependent task count, and disable the Delete button when dependencies exist

## 3. Verification

- [x] 3.1 Build the solution and verify no compilation errors
- [x] 3.2 Manually verify: navigate to Delete page for a category/priority with dependent tasks — confirm warning is shown and Delete button is disabled
- [x] 3.3 Manually verify: navigate to Delete page for a category/priority with no dependent tasks — confirm no warning and Delete button is enabled
