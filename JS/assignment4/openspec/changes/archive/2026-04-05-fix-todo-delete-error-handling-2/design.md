## Context

The application uses `DeleteBehavior.Restrict` globally (`BaseDbContext.OnModelCreating`), preventing deletion of entities with dependent records. The API controllers already handle this gracefully — they pre-check dependent counts and return 409 Conflict. The MVC controllers have no such handling, causing unhandled `DbUpdateException` and a 500 error page when users try to delete a category or priority with tasks.

## Goals / Non-Goals

**Goals:**
- MVC delete operations for TodoCategories and TodoPriorities gracefully handle dependent record constraints
- Users see a clear error message with the count of dependent tasks
- The Delete confirmation page (GET) proactively shows the dependent task count as a warning
- Pattern mirrors what the API controllers already do

**Non-Goals:**
- Cascade delete or reassignment of dependent tasks (the restrict behavior is intentional)
- Generic error handling middleware for all constraint violations
- Changes to any other controllers or entities beyond TodoCategories and TodoPriorities

## Decisions

### 1. Pre-check count in both GET and POST actions

**Decision:** Query `_context.TodoTasks.CountAsync(t => t.TodoCategoryId == id)` (and equivalent for priorities) in both the `Delete` GET action and `DeleteConfirmed` POST action.

**Rationale:** The GET action shows the warning proactively so the user knows before clicking Delete. The POST action re-checks to handle race conditions (tasks added between page load and form submit). This mirrors the API pattern from `WebApp/ApiControllers/TodoCategoriesController.cs`.

**Alternative considered:** Only checking in POST and showing error after failure — rejected because proactive warning is better UX.

### 2. Use ViewData for error message passing

**Decision:** Pass error information via `ViewData["ErrorMessage"]` and `ViewData["DependentTaskCount"]`, then re-render the Delete view with the error displayed.

**Rationale:** This is the simplest approach that works with the existing scaffolded views. No need for a view model wrapper or TempData redirect pattern — the controller already loads the entity and returns `View(entity)`, so adding ViewData is minimal change.

**Alternative considered:** A dedicated view model wrapping the entity plus error state — rejected as over-engineered for this case.

### 3. Bootstrap alert for error display in views

**Decision:** Add a conditional Bootstrap `alert-danger` div at the top of the Delete views that renders when `ViewData["ErrorMessage"]` is set. The alert shows the error message including the dependent task count. Disable the Delete button when dependencies exist.

**Rationale:** Consistent with Bootstrap styling already used throughout the app. The alert is visible and clear without requiring additional CSS or JS.

### 4. DbUpdateException catch as safety net in POST

**Decision:** Wrap `SaveChangesAsync()` in a try/catch for `DbUpdateException` as a fallback, even though the pre-check should prevent reaching it.

**Rationale:** Race condition safety — if a task is created between the count check and the delete, the exception catch prevents a 500 error. Same pattern used in the API controllers.

## Risks / Trade-offs

- **[Race condition]** → Mitigated by the DbUpdateException catch block as fallback after pre-check
- **[Two DB queries on GET]** → Acceptable; the count query is lightweight and the UX benefit of proactive warning justifies it
- **[ViewData string keys]** → Minor fragility, but consistent with existing patterns in the app and contained to two controller/view pairs
