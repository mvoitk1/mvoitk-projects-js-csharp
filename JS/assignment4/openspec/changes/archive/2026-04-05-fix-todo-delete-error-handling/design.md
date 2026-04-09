## Context

The application globally disables cascade delete via `DeleteBehavior.Restrict` in `BaseDbContext.OnModelCreating()` (`com.akaver.DAL.Base.EF/BaseDbContext.cs:56-61`). This means attempting to delete a `TodoPriority` or `TodoCategory` that has associated `TodoTask` records causes the database to reject the operation with a foreign key constraint violation.

Currently, `DeleteTodoPriority` (`WebApp/ApiControllers/TodoPrioritiesController.cs:111-127`) and `DeleteTodoCategory` (`WebApp/ApiControllers/TodoCategoriesController.cs:123-140`) call `SaveChangesAsync()` without any exception handling. The unhandled `DbUpdateException` propagates as a 500 Internal Server Error.

## Goals / Non-Goals

**Goals:**
- Return 409 Conflict with a descriptive error message when deleting a priority or category that has dependent tasks
- Add integration tests verifying the 409 response when dependents exist

**Non-Goals:**
- Changing the global cascade delete policy
- Adding cascade delete or automatic orphan cleanup for TodoTask records
- Modifying any other delete endpoints beyond TodoPriority and TodoCategory

## Decisions

### Decision 1: Pre-check dependent count, then delete with exception handling

Before attempting deletion, query the count of dependent `TodoTask` records for the entity. If the count is > 0, return 409 Conflict immediately with the count in the response. If the count is 0, proceed with deletion wrapped in a try-catch for `DbUpdateException` as a safety net (handles the TOCTOU race where a task is created between the count query and the delete).

**Why pre-check instead of only catching exceptions?** The response must include the count of dependent records. A `DbUpdateException` doesn't carry this information, so a pre-check query is necessary. The try-catch remains as a fallback for the rare race condition — in that case the count in the error message may say 0 but the 409 status still correctly communicates the conflict.

**Why 409 Conflict?** The deletion cannot be completed because the resource's current state (having dependents) conflicts with the requested operation. This aligns with RFC 9110 §15.5.10. Alternatives considered:
- 400 Bad Request — not appropriate; the request itself is well-formed
- 422 Unprocessable Entity — closer, but typically for validation errors on input
- 409 Conflict — best fit for state-based rejection

### Decision 2: Return a JSON error body

Return a `ProblemDetails`-style response body with a human-readable message that includes the dependent record count, e.g. `"Entity cannot be deleted because it has 3 dependent TodoTask record(s)"`. This keeps the API consistent and gives clients actionable information.

## Risks / Trade-offs

- **[Risk] Catching broad DbUpdateException may mask other DB errors** → Mitigation: The catch block returns 409 specifically for this scenario. If needed in the future, we can inspect the inner exception for specific constraint violation codes, but for now the only expected `DbUpdateException` on delete is FK violation.
- **[Risk] Error message doesn't specify which dependents block deletion** → Acceptable trade-off for simplicity. Clients can query tasks filtered by category/priority to discover dependents.
