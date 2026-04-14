## ADDED Requirements

### Requirement: Tasks SHALL support full CRUD with validation of required fields and relations
Tasks SHALL require id, title, status, priorityId, and categoryId; create/update SHALL validate foreign keys (priority, category) and recurrence/dependency references.

#### Scenario: Create task with valid relations succeeds
- **WHEN** a task DTO with valid priorityId and categoryId is submitted
- **THEN** the task SHALL be persisted and retrievable with the same IDs

#### Scenario: Create task with missing category fails
- **WHEN** a task DTO omits categoryId
- **THEN** the system SHALL reject the create with a validation error

### Requirement: Cascade delete SHALL remove dependent entities
Deleting a task SHALL also delete its subtasks/checklist items, recurrence instances, dependency edges (incoming and outgoing), attachments, comments, and reminders in the same commit.

#### Scenario: Delete task clears dependents
- **WHEN** a task with subtasks, dependencies, recurrence instances, attachments, comments, and reminders is deleted
- **THEN** all those dependents SHALL be removed in the same batch commit

### Requirement: Task updates SHALL preserve referential integrity
Updates SHALL reject changes that introduce invalid foreign keys or orphaned dependency edges; moving a task to a different category/priority SHALL validate existence.

#### Scenario: Update with invalid priority is rejected
- **WHEN** a task update sets priorityId to a non-existent priority
- **THEN** the update SHALL be rejected and the task SHALL remain unchanged

### Requirement: Recurrence rules SHALL be stored and applied without duplicating occurrences
Tasks MAY include recurrence rules; stored rules SHALL not duplicate occurrences in storage. Generated instances SHALL reference the source task ID.

#### Scenario: Recurring task stores rule and generates instances on read
- **WHEN** a task with a weekly recurrence rule is saved
- **THEN** the recurrence rule SHALL be stored with the task, and instances SHALL be produced on read without storing each occurrence
