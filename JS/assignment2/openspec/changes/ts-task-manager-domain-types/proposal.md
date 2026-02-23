## Why

Migrating the existing browser-based task manager from JavaScript/HTML to TypeScript with strict typing enables safer iteration, clearer contracts, and unlocks new features (recurrence, dependencies, richer stats/search/sorting) without regressing current behaviors from assignment1.

## What Changes

- Add comprehensive TypeScript domain model (interfaces/enums/type aliases) for tasks, projects/boards/lists, categories, priorities, tags/labels, users/assignees, comments, attachments, reminders, recurrence, dependencies, checklist/subtasks, audit fields.
- Preserve and formalize existing behaviors from assignment1 (CRUD, search, filtering, sorting, calendar/dashboard views, localStorage persistence, validation) under typed contracts.
- Introduce new capabilities: recurring tasks, task dependency graph (blocking/related), reminders, extended statistics (counts by status/category/priority, overdue), and enhanced search/sort fields.
- Plan generic utility type signatures (e.g., typed sort, filter/search matcher, recurrence expansion, dependency cycle detection, stats aggregation) to support the domain.
- Ensure Category ± Task ± Priority relationships are explicit in types and allow many-to-many links for tags and assignees.

## Capabilities

### New Capabilities
- `ts-domain-modeling`: Define strict TypeScript interfaces/enums for all task management entities and relationships (tasks, categories, priorities, tags, users, comments, attachments, reminders, recurrence, dependencies, checklist/subtasks, audit fields) with required fields and associations.
- `recurrence-and-dependencies`: Specify type contracts for recurring tasks (rules, instances) and task dependency types (blocking, related, duplicate) with validation-friendly structures.
- `typed-search-sort-stats`: Establish typed shapes for search/filter/sort inputs and output statistics (status/category/priority counts, overdue metrics) aligned to calendar/dashboard needs.

### Modified Capabilities
- _None._

## Impact

- Affects domain typing across the app: state models, localStorage schemas, validators, UI bindings (dashboard, list, calendar, command palette), and future service abstractions.
- Requires copying assignment1 assets into assignment2 (read-only source) and converting JS modules to TS with strict mode, updating references to use the new typed models.
- Sets contracts for future implementation work (design/specs/tasks) but introduces no runtime changes in this phase.
