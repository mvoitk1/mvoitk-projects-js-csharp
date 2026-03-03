## ADDED Requirements

### Requirement: Recurrence rule representation
The system SHALL define an `IRecurrenceRule` interface to describe repeating tasks, including frequency (`ERecurrenceFrequency`), interval, optional count or until date, optional time-of-day, optional day-of-week/month/year patterns, and links to the source task id. Recurring instances SHALL reference the source recurrence id.

#### Scenario: Daily recurrence with end date
- **WHEN** a task is set to recur daily until a specific end date
- **THEN** the recurrence rule SHALL encode frequency=daily, an until date, and interval=1, enabling generation of occurrence dates consistent with the rule.

### Requirement: Dependency modeling with types
The system SHALL define an `IDependency` interface linking a task to another task with a dependency type (`EDependencyType`, e.g., blocks, blocked-by, relates-to, duplicates), and optional metadata (note, createdAt, createdBy). Tasks SHALL store an array of dependency entries.

#### Scenario: Blocking dependency prevents premature completion
- **WHEN** a task includes a dependency of type `blocks` pointing to another task
- **THEN** the consuming logic SHALL be able to interpret that the current task must complete before the blocked task can be marked done, based on the dependency type and target task id.

### Requirement: Checklist and subtasks linkage
The system SHALL represent checklist items/subtasks via `IChecklistItem` with id, parent task id, title, completed flag, optional assigneeId, and timestamps. Subtasks are checklist items that may themselves reference a task id or inherit parent context.

#### Scenario: Subtask completion tracked independently
- **WHEN** a task has multiple checklist items marked complete independently
- **THEN** each checklist item SHALL maintain its own completed flag and completedAt timestamp without altering sibling items, while the parent task can derive completion status from the collection.
