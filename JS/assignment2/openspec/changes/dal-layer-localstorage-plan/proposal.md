## Why

We need a clear DAL plan that reuses existing domain interfaces while persisting to browser localStorage, enabling full CRUD plus cascading deletes, typed search, and repository/UOW patterns to keep the new TypeScript migration coherent and maintainable.

## What Changes
- Introduce a localStorage-backed DAL aligned to existing domain interfaces for tasks, categories, priorities, recurrence, dependencies, and related entities.
- Define repositories for core aggregates (task, category, priority, recurrence/dependency graph) with consistent DTO ↔ domain mapping.
- Add a Unit of Work to coordinate multi-entity operations (create/update/delete) and enforce cascade delete of dependents/recurrence/subtasks/attachments/comments.
- Provide typed search/filter/sort support across task collections, leveraging existing domain fields and constraints.
- Ensure CRUD operations (create/read/update/delete) are complete, validated, and persist via localStorage with strict TypeScript types.
- Document storage schemas, lifecycle rules, and concurrency/conflict handling strategy appropriate for localStorage.

## Capabilities

### New Capabilities
- `localstorage-dal-layer`: LocalStorage-backed persistence layer that implements existing domain interfaces and maps domain models to stored DTOs.
- `repository-uow`: Repository set (tasks, categories, priorities, recurrence/dependencies) plus Unit of Work to coordinate transactional operations and cascades.
- `task-crud-cascade`: Full CRUD with cascade delete across dependencies, recurrence instances, subtasks/checklists, attachments, reminders, and comments.
- `task-search-filter-sort`: Typed search, filtering, and sorting over tasks using domain fields (status, priority, due dates, categories, recurrence/dependency state) with pagination-friendly querying.

### Modified Capabilities
- (none)

## Impact
- Affects DAL design in `src` (repository/UOW abstractions, mapping utilities, localStorage gateway) and spec/design/task artifacts for the change.
- Introduces storage schema definitions for localStorage keys/shapes and mapping between domain interfaces and persisted DTOs.
- Requires alignment with existing domain models/specs for tasks, recurrence, dependencies, search/sort/stats, categories, and priorities to ensure compatibility.
- Influences testing strategy (unit tests for repositories/UOW, cascade behaviors, and search) and potential future migration paths from localStorage to other stores.
