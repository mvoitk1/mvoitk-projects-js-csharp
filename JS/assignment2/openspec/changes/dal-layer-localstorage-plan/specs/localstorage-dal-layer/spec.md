## ADDED Requirements

### Requirement: Collections SHALL be stored under namespaced keys with versioned envelopes
Each persisted collection SHALL be stored under a deterministic namespaced localStorage key (e.g., `tm.tasks`, `tm.categories`, `tm.priorities`, `tm.dependencies`, `tm.recurrence`, `tm.comments`, `tm.attachments`, `tm.reminders`) containing an envelope `{ version: number, updatedAt: ISO string, items: TDto[] }`.

#### Scenario: Successful load of a namespaced collection
- **WHEN** the DAL loads `tm.tasks` with a valid envelope and matching schema version
- **THEN** it SHALL deserialize items into DTOs and return them to repositories without mutation

#### Scenario: Missing collection initializes empty envelope
- **WHEN** the DAL loads a missing key such as `tm.comments`
- **THEN** it SHALL initialize `{ version: 1, updatedAt: now, items: [] }` and persist it before returning

### Requirement: Batch writes SHALL be atomic per commit
DAL commit operations SHALL write all modified collections in a single logical batch; if any collection fails validation, no collection SHALL be updated and the prior state SHALL remain intact.

#### Scenario: Failed collection validation aborts batch
- **WHEN** the DAL attempts to commit tasks and dependencies but dependency validation fails
- **THEN** no collection key SHALL be written, and previous values SHALL be preserved

### Requirement: Envelope validation SHALL reject malformed or stale payloads
On read, the DAL SHALL validate envelope shape and numeric version; on write, it SHALL require the caller’s version to match the stored version, incrementing on success and raising a concurrency error on mismatch.

#### Scenario: Version mismatch raises concurrency error
- **WHEN** a commit is attempted with version 3 but the stored envelope is at version 4
- **THEN** the DAL SHALL reject the commit and return a concurrency error without persisting changes

#### Scenario: Malformed envelope is quarantined
- **WHEN** the DAL reads a collection whose value is not valid JSON or missing required fields
- **THEN** it SHALL quarantine the value (e.g., move to `tm._corrupt.<collection>.<timestamp>`) and reinitialize a fresh empty envelope
