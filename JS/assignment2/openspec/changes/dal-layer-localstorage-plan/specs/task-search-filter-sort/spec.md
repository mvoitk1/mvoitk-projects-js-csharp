## ADDED Requirements

### Requirement: In-memory indexes SHALL power search/filter/sort
The system SHALL build in-memory indexes over loaded DTOs to support filtering by status, priority, category, tag/label, due date windows, recurrence presence, and dependency-blocked state, and sorting by due date, priority, createdAt, and updatedAt.

#### Scenario: Filter returns only matching records
- **WHEN** querying for tasks with status = "in-progress" and priority = "high"
- **THEN** only tasks matching both criteria SHALL be returned

#### Scenario: Sort by due date ascending
- **WHEN** a sort by dueDate ascending is applied
- **THEN** results SHALL be ordered from earliest dueDate to latest

### Requirement: Dependency-blocked tasks SHALL be detectable
Queries SHALL be able to filter tasks that are blocked by unresolved dependencies (i.e., have incoming edges from incomplete tasks).

#### Scenario: Blocked filter returns only dependency-blocked tasks
- **WHEN** filtering for blocked tasks
- **THEN** tasks with at least one incomplete dependency SHALL be returned, and tasks without incomplete dependencies SHALL be excluded

### Requirement: Recurrence-aware date filtering SHALL include generated instances
Date range filters (overdue, upcoming) SHALL consider recurrence-generated instances as if materialized, without storing each occurrence.

#### Scenario: Upcoming filter includes recurrence instances
- **WHEN** filtering for tasks due in the next 7 days
- **THEN** tasks with recurrence rules producing instances in that window SHALL be included

### Requirement: Statistics SHALL be derivable from indexed collections
The system SHALL derive counts and aggregates (by status, priority, category, overdue, upcoming, dependency-blocked) from in-memory collections without persisting cached stats.

#### Scenario: Stats report aligns with current data
- **WHEN** tasks are updated and a stats request is made
- **THEN** the returned stats SHALL reflect the latest in-memory state without requiring a refresh from storage
