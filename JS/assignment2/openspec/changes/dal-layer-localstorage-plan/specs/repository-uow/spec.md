## ADDED Requirements

### Requirement: Repositories SHALL provide type-safe CRUD over DTOs aligned to domain interfaces
Each repository (tasks, categories, priorities, recurrence, dependencies, comments, attachments, reminders) SHALL expose create/read/update/delete operations over DTOs that map 1:1 to domain interfaces (interfaces prefixed `I*`, enums `E*`), enforcing ID integrity and required fields.

#### Scenario: Successful typed create
- **WHEN** a valid task DTO with required fields (id, title, status, priorityId, categoryId) is provided
- **THEN** the tasks repository SHALL persist it and return the stored DTO with the same ID

#### Scenario: Reject create with missing required foreign key
- **WHEN** a task DTO references a non-existent `categoryId`
- **THEN** the repository SHALL reject the create and report a foreign key violation

### Requirement: Unit of Work SHALL batch changes and validate before commit
The Unit of Work SHALL aggregate pending creates/updates/deletes across repositories, validate referential integrity (including dependencies and recurrence), detect dependency cycles, and commit via the DAL in a single atomic batch.

#### Scenario: Cycle detection blocks commit
- **WHEN** pending dependency edges form a cycle
- **THEN** the Unit of Work SHALL reject the commit and no changes SHALL be written

#### Scenario: Missing dependent entity blocks commit
- **WHEN** a reminder references a task that is deleted in the same batch without cascade registration
- **THEN** the Unit of Work SHALL reject the commit for failed referential integrity

### Requirement: Cascade delete rules SHALL be enforced at commit
When deleting a task, the Unit of Work SHALL cascade delete dependents: subtasks/checklist items, recurrence instances, dependency edges (incoming/outgoing), attachments, comments, reminders, and child tasks in dependency trees as configured.

#### Scenario: Delete task removes all dependents
- **WHEN** a task with recurrence instances, dependency edges, comments, attachments, and reminders is deleted
- **THEN** the commit SHALL remove the task and all related records in the same batch

### Requirement: Concurrency policy SHALL use envelope versions
The Unit of Work SHALL read collection envelope versions at load and require unchanged versions at commit; on version mismatch it SHALL abort and surface a concurrency error with no writes applied.

#### Scenario: Concurrency mismatch aborts commit
- **WHEN** another tab updates `tm.tasks` between load and commit
- **THEN** the Unit of Work SHALL detect the version change and abort the commit without partial writes
