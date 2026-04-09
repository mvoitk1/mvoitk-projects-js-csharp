## 1. Error Handling in Controllers

- [x] 1.1 In `DeleteTodoPriority` (`WebApp/ApiControllers/TodoPrioritiesController.cs:124`): query dependent TodoTask count, return `Conflict()` with count if > 0, wrap `SaveChangesAsync()` in try-catch for `DbUpdateException` as fallback
- [x] 1.2 In `DeleteTodoCategory` (`WebApp/ApiControllers/TodoCategoriesController.cs:137`): query dependent TodoTask count, return `Conflict()` with count if > 0, wrap `SaveChangesAsync()` in try-catch for `DbUpdateException` as fallback

## 2. Integration Tests

- [x] 2.1 Add test in `TodoPrioritiesControllerTest.cs`: create a priority, create a task using that priority, attempt to delete the priority, assert 409 Conflict and verify response contains dependent count
- [x] 2.2 Add test in `TodoCategoriesControllerTest.cs`: create a category, create a task using that category, attempt to delete the category, assert 409 Conflict and verify response contains dependent count
