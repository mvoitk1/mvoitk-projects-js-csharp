## ADDED Requirements

### Requirement: Typed task entity contract
The system SHALL define an `ITask` interface capturing task identity, core fields (title, description), lifecycle timestamps (createdAt, updatedAt, completedAt), scheduling (dueDate), status (`ETaskStatus`), priority (`ETaskPriority`), associations (projectId/boardId/listId, categoryId, priorityId), recurrence reference, dependency references, checklist/subtasks, tags, comments, attachments, reminders, assignees, and audit fields (createdBy, updatedBy).

#### Scenario: New task shape is strictly typed
- **WHEN** a task is created or loaded into state
- **THEN** it SHALL conform to `ITask` with all required fields typed and optional fields explicitly marked optional, including associations to category/priority and collections for tags, assignees, dependencies, comments, attachments, reminders, and checklist items.

### Requirement: Relationship cardinalities are explicit
The system SHALL represent relationships with explicit multiplicities: tasks belong to exactly one category and one priority; tasks may belong to one project/board and one list; tasks may link to zero or many tags (many-to-many), zero or many assignees (many-to-many with users), zero or many dependencies, zero or many checklist items, zero or many comments, zero or many attachments, and zero or many reminders.

#### Scenario: Tags and assignees support many-to-many
- **WHEN** a task is associated with multiple tags and multiple assignees
- **THEN** the task SHALL store tagIds and assigneeIds as arrays of identifiers, and tags/users SHALL not require backpointers beyond optional taskId collections for convenience.

### Requirement: Supporting entities are strongly typed
The system SHALL define interfaces and enums for supporting entities: `ICategory`, `IPriority`, `IProject`, `IBoard`, `IList`, `ITag`, `IUser`, `IComment`, `IAttachment`, `IChecklistItem`, `IReminder`, `IAuditMetadata`, `IRecurrenceRule` reference, and `IDependency`, using enums prefixed with `E` and interfaces prefixed with `I`.

#### Scenario: Category and priority enforce required fields
- **WHEN** a category or priority object is instantiated or persisted
- **THEN** it SHALL include a stable identifier, name/label, optional description, ordering/index, color metadata, and audit fields, with priorities additionally declaring their `ETaskPriority` value.
