## ADDED Requirements

### Requirement: Typed search and filter inputs
The system SHALL define typed search/filter inputs that accept text queries over title/description/tags, structured filters for status (`ETaskStatus`), priority (`ETaskPriority`), categoryId, project/board/list ids, assigneeIds, tagIds, date ranges (created, updated, due, completed), and recurrence flags, ensuring all filter fields are explicitly typed.

#### Scenario: Combined tag and date filtering
- **WHEN** a search applies tagIds and a dueDate range filter together
- **THEN** the filter input shape SHALL represent both constraints simultaneously, enabling deterministic filtering over typed fields without ambiguity.

### Requirement: Typed sorting contracts
The system SHALL define sort descriptors with allowed fields (`ESortField`, e.g., priority, status, dueDate, createdAt, updatedAt, completedAt, title) and direction (`ESortDirection`), ensuring sorting operates on well-typed keys consistent with task properties.

#### Scenario: Sort by priority then due date
- **WHEN** a sort descriptor specifies primary sort by priority and secondary by dueDate
- **THEN** the sorting contract SHALL allow ordered descriptors referencing `ETaskPriority` weights and date fields without falling back to untyped keys.

### Requirement: Typed statistics shapes
The system SHALL define typed statistic outputs for dashboard/calendar needs, including counts by status, category, priority, overdue tasks, upcoming tasks within a window, and totals. Each stat structure SHALL declare fields and value types explicitly.

#### Scenario: Overdue count derivation
- **WHEN** computing stats for dashboard display
- **THEN** the stats output shape SHALL include an overdue count derived from tasks with dueDate before now and status not completed/cancelled, ensuring consuming views receive a typed numeric field.
