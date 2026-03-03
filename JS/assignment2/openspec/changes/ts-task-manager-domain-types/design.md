## Context

- Current app exists in assignment1 as a pure JS/HTML localStorage task manager with CRUD, filters, search, sorting, calendar view, dashboard stats, command palette, and validation. Migration must not modify assignment1; assets will be copied into assignment2 and converted to strict TypeScript.
- New requirements: comprehensive TypeScript domain types (I*/E* prefixes), recurrence, dependencies, reminders, richer stats/search/sorting, and explicit Category ± Task ± Priority relationships with many-to-many tags/assignees. At least three generic utility functions are expected.
- Tech stack: TypeScript (strict), browser/localStorage persistence. UI is vanilla HTML/JS; TypeScript will enhance types and safety without introducing a framework in this phase.

## Goals / Non-Goals

**Goals:**
- Define typed domain contracts for tasks, projects/boards/lists, categories, priorities, tags, users/assignees, comments, attachments, reminders, recurrence, dependencies, checklist/subtasks, and audit metadata.
- Map existing JS behaviors to typed interfaces/enums without regressions in features (CRUD, search, filters, sorting, calendar/dashboard, command palette, localStorage persistence, validation).
- Specify how recurrence, dependencies, reminders, and stats integrate with the domain types and storage schemas.
- Outline typed utility shapes (sorting, filtering/search, recurrence expansion, dependency checks, stats aggregation) to be implemented later.

**Non-Goals:**
- No runtime implementation in this phase; no UI rewrites; no backend introduction. No changes to assignment1.

## Decisions

- **Type prefixing and strict mode**: All interfaces prefixed with `I`, enums with `E`; project compiled with `strict` to enforce null/undefined safety and typed collections. Rationale: aligns with config rules and reduces runtime type errors.
- **Entity IDs and references**: Use string IDs for portability across storage (localStorage keys, potential future backend). Tasks reference related entities by id arrays (tags, assignees, dependencies) and singular ids (category, priority, project/board/list). Rationale: keeps storage flat and serializable.
- **Recurrence representation**: Store recurrence as an `IRecurrenceRule` on the source task plus `recurrenceId` on generated instances. Rationale: separates template rule from occurrences and allows regeneration.
- **Dependency modeling**: Use `IDependency` entries with typed `EDependencyType` (blocks, blocked-by, relates-to, duplicates). Rationale: supports validation (cycle detection) and UI hints for blocking.
- **Checklist vs. subtasks**: Model checklist items via `IChecklistItem` with optional link to a subtask id; keep them lightweight and embedded under the parent task. Rationale: avoids separate task objects while supporting completion tracking.
- **Sorting and filtering contracts**: Define `ESortField`/`ESortDirection` and typed filter input shapes to ensure UI and data layer share the same allowed keys. Rationale: prevents ad-hoc string keys and supports stable search/filter behavior.
- **Statistics shape**: Define explicit structures for counts by status/category/priority and overdue/upcoming metrics. Rationale: dashboard/calendar can consume typed stats without recomputing shapes.
- **Utilities (type-first)**: Plan generic signatures for sort, filter/search matcher, recurrence expansion, dependency cycle detection, and stats aggregation. Rationale: enforces typed pipelines and reusable behaviors.

## Risks / Trade-offs

- **Risk: Type drift from existing JS data** → Mitigation: add migration/validation step when loading legacy localStorage to conform to new interfaces (with defaults for new fields like recurrence/dependencies).
- **Risk: Recurrence/occurrence consistency** → Mitigation: separate rule vs. instances; provide deterministic expansion utility and store linkage via recurrenceId.
- **Risk: Dependency cycles or invalid references** → Mitigation: dependency cycle detector utility; validation on load/save to ensure referenced task ids exist.
- **Risk: Strict null checks breaking legacy code** → Mitigation: incremental typing with safe defaults and narrowings at boundaries (storage, UI inputs).
- **Risk: Stat accuracy on large datasets** → Mitigation: typed aggregators; optional memoization when wiring implementation.

## Migration Plan

1) Copy assignment1 assets into assignment2 (read-only source) without modification.
2) Introduce TypeScript config (strict already present) and convert modules to TS, applying I*/E* naming and typed models.
3) Define domain types/enums per specs; update storage layer typing and validation to coerce legacy records (defaults for new fields).
4) Wire UI/state to typed contracts, replacing untyped access with typed selectors/utilities.
5) Add typed utilities (sort, filter/search matcher, recurrence expansion, dependency cycle detection, stats aggregation) and integrate progressively.
6) Validate recurrence/dependency data paths and update command palette/CLI flows to use typed inputs.

## Open Questions

- How to version localStorage data when introducing recurrence/dependencies to existing records (e.g., schema version field)?
- Should subtasks ever be promoted to full tasks with their own dependencies/recurrence, or remain checklist-only?
- Do reminders require multiple channels (email/push) or remain in-app/local only in this phase?
