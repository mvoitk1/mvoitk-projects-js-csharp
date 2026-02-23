## 1. Repo setup and baselines

- [ ] 1.1 Copy assignment1 assets into assignment2 (read-only source) without modification.
- [ ] 1.2 Enable strict TypeScript settings and ensure tsconfig aligns with interfaces/enums prefix rules (I*/E*).

## 2. Domain types and enums

- [ ] 2.1 Define core enums: `ETaskStatus`, `ETaskPriority`, `EDependencyType`, `ERecurrenceFrequency`, `EReminderType`, `ESortField`, `ESortDirection`.
- [ ] 2.2 Define core interfaces: `ITask`, `ICategory`, `IPriority`, `IProject`, `IBoard`, `IList`, `ITag`, `IUser`, `IComment`, `IAttachment`, `IReminder`, `IChecklistItem`, `IAuditMetadata`.
- [ ] 2.3 Define recurrence and dependency shapes: `IRecurrenceRule`, `IDependency`, and linkages on task entities.

## 3. Relationships and validation modeling

- [ ] 3.1 Encode relationships: one-to-many (category→tasks, priority→tasks, project/board/list→tasks), many-to-many (tasks↔tags, tasks↔assignees), dependencies array, checklist/subtasks, comments, attachments, reminders.
- [ ] 3.2 Specify validation-friendly optional/required fields and defaulting strategy for legacy data (createdAt/updatedAt, dueDate, recurrence/dependencies placeholders).

## 4. Persistence and migration notes

- [ ] 4.1 Define typed shapes for storage (localStorage schemas) including schema versioning placeholder for new fields (recurrence, dependencies, reminders).
- [ ] 4.2 Plan coercion of legacy records from assignment1 into typed shapes with safe defaults and validation steps.

## 5. Utility type signatures (no implementation yet)

- [ ] 5.1 Draft generic sort descriptor signature using `ESortField`/`ESortDirection` and comparator helpers.
- [ ] 5.2 Draft typed filter/search matcher signature supporting text, tags, status, priority, category, project/board/list, assignees, date ranges, recurrence flags.
- [ ] 5.3 Draft recurrence expansion signature and dependency cycle detection signature.
- [ ] 5.4 Draft statistics aggregation signature for status/category/priority counts, overdue/upcoming metrics.

## 6. UI/state wiring plan

- [ ] 6.1 Identify JS modules to convert to TS and map to typed models (tasks list, calendar, dashboard, command palette, storage, validation).
- [ ] 6.2 Plan updates to command palette/CLI flows to accept typed inputs (status/priority enums, tag/assignee ids, recurrence/dependencies data).

## 7. Documentation and checks

- [ ] 7.1 Cross-check types against specs and design for completeness; adjust for any gaps.
- [ ] 7.2 Validate naming conventions (I*/E* prefixes) and ensure strict-null checks compliance in type definitions.
