# MVC Delete Error Handling

## Purpose

Defines how MVC delete confirmation pages and delete actions handle dependent record constraints for TodoCategories and TodoPriorities, ensuring users receive clear feedback when deletion is blocked by dependent tasks.

## Requirements

### Requirement: MVC Delete confirmation page shows dependent task count as warning

The TodoCategories and TodoPriorities `Delete` confirmation pages (GET) SHALL display the number of dependent TodoTask records associated with the entity being deleted. When dependent tasks exist, the page SHALL show a warning alert indicating that the entity cannot be deleted and stating the exact count of dependent tasks. The Delete button SHALL be disabled when dependent tasks exist.

#### Scenario: Delete confirmation page for TodoCategory with dependent tasks
- **WHEN** a user navigates to the Delete confirmation page for a TodoCategory that has 3 associated TodoTask records
- **THEN** the page displays a warning alert stating the category cannot be deleted because it has 3 dependent task(s), and the Delete button is disabled

#### Scenario: Delete confirmation page for TodoCategory with no dependent tasks
- **WHEN** a user navigates to the Delete confirmation page for a TodoCategory that has no associated TodoTask records
- **THEN** the page displays the standard delete confirmation without any warning, and the Delete button is enabled

#### Scenario: Delete confirmation page for TodoPriority with dependent tasks
- **WHEN** a user navigates to the Delete confirmation page for a TodoPriority that has 5 associated TodoTask records
- **THEN** the page displays a warning alert stating the priority cannot be deleted because it has 5 dependent task(s), and the Delete button is disabled

#### Scenario: Delete confirmation page for TodoPriority with no dependent tasks
- **WHEN** a user navigates to the Delete confirmation page for a TodoPriority that has no associated TodoTask records
- **THEN** the page displays the standard delete confirmation without any warning, and the Delete button is enabled

### Requirement: MVC DeleteConfirmed action handles dependent record constraint gracefully

The TodoCategories and TodoPriorities `DeleteConfirmed` POST actions SHALL check for dependent TodoTask records before attempting deletion. When dependent records exist, the action SHALL re-render the Delete view with an error message including the count of dependent tasks instead of throwing an unhandled exception. As a safety net, the action SHALL also catch `DbUpdateException` and display an appropriate error message.

#### Scenario: Delete TodoCategory POST with dependent tasks (pre-check)
- **WHEN** a user submits the Delete form for a TodoCategory that has dependent TodoTask records
- **THEN** the controller re-renders the Delete view with an error message stating the category cannot be deleted and showing the dependent task count

#### Scenario: Delete TodoPriority POST with dependent tasks (pre-check)
- **WHEN** a user submits the Delete form for a TodoPriority that has dependent TodoTask records
- **THEN** the controller re-renders the Delete view with an error message stating the priority cannot be deleted and showing the dependent task count

#### Scenario: Delete TodoCategory POST with no dependent tasks succeeds
- **WHEN** a user submits the Delete form for a TodoCategory that has no dependent TodoTask records
- **THEN** the category is deleted and the user is redirected to the Index page

#### Scenario: Delete TodoPriority POST with no dependent tasks succeeds
- **WHEN** a user submits the Delete form for a TodoPriority that has no dependent TodoTask records
- **THEN** the priority is deleted and the user is redirected to the Index page

#### Scenario: Delete TodoCategory POST catches DbUpdateException as fallback
- **WHEN** a user submits the Delete form for a TodoCategory and the pre-check passes but a `DbUpdateException` occurs during `SaveChangesAsync` (race condition)
- **THEN** the controller re-renders the Delete view with an error message indicating deletion failed due to dependent records
