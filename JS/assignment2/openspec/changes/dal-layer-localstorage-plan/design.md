## Context

- Migrating the existing browser-based task manager (assignment1 JS/HTML) to TypeScript with strict mode while adding recurrence, dependencies, statistics, search, sorting, and category–task–priority relationships.
- Persistence target is browser `localStorage`; no server or backend services are available.
- Must align with existing domain interfaces/specs (tasks, categories, priorities, recurrence/dependencies, search/sort/stats) and enforce DTO ↔ domain mapping with `I*` interfaces and `E*` enums.
- Need repository + Unit of Work to coordinate multi-entity operations, cascade delete, and maintain referential integrity across tasks, subtasks/checklists, comments, attachments, reminders, recurrence instances, and dependency edges.
- Performance and concurrency are bounded by localStorage: synchronous API, ~5–10MB storage, single-threaded access; optimistic versioning can prevent stale writes.

## Goals / Non-Goals

**Goals:**
- Provide a localStorage-backed DAL that implements existing domain interfaces for tasks, categories, priorities, recurrence/dependencies, comments, attachments, reminders, and stats.
- Enable full CRUD with deterministic cascade delete (tasks → subtasks/checklist → attachments/comments/reminders; dependency edges; recurrence instances).
- Implement repository interfaces plus a Unit of Work that batches changes, validates referential integrity, and applies cascades atomically per commit.
- Support typed search/filter/sort over task collections using in-memory indexes derived from localStorage data (status, priority, due dates, categories/tags, recurrence, dependency state).
- Define DTO schemas, storage keys, versioning/metadata, and mapping utilities to/from domain objects.
- Prepare for future migration to other storage backends by isolating storage concerns behind repositories and mappers.

**Non-Goals:**
- Real-time collaboration or multi-tab synchronization beyond optimistic version checks and reload detection.
- End-to-end encryption or secure remote sync.
- Backend services, offline sync queues, or conflict-free replicated data types.
- Full-text search; only structured filters/sorts are in scope.

## Decisions

- **Storage keying and layout**: Use one namespaced key per aggregate collection (e.g., `tm.tasks`, `tm.categories`, `tm.priorities`, `tm.recurrence`, `tm.dependencies`, `tm.comments`, `tm.attachments`, `tm.reminders`) storing a JSON payload `{ version, updatedAt, items: TDto[] }`. *Rationale*: Minimizes localStorage key churn and simplifies version metadata. *Alternative*: Per-entity keys increase overhead and complicate batch reads.
- **DTO schema vs domain**: Persist flattened DTOs with explicit foreign keys (IDs) and normalized references; reconstruct domain aggregates via mappers. *Rationale*: Keeps storage compact and referentially checkable; domain objects can compose richer structures. *Alternative*: Store hydrated domain graphs, risking circular refs and larger payloads.
- **Unit of Work**: Accumulate change-sets in memory (creates/updates/deletes per collection), validate integrity, apply cascades, then commit all collections in a single write batch. *Rationale*: Ensures atomic multi-entity operations and consistent cascades. *Alternative*: Per-repo writes risk partial updates on failure.
- **Cascade delete strategy**: Precompute affected IDs (dependencies, recurrence instances, subtasks/checklists, attachments, comments, reminders) before commit; apply removals in the same batch. *Rationale*: Deterministic cleanup without orphaned records. *Alternative*: Lazy cleanup on read would leak stale references.
- **Search/filter/sort**: Load collections into memory once per DAL session; build derived indexes (by status, priority, due date buckets, category, tag/label, dependency state, recurrence state). Queries operate on in-memory arrays with typed predicates/comparators, not per-call localStorage scans. *Alternative*: Direct scans on each call would be slower and repetitive.
- **Recurrence representation**: Store recurrence rules as DTOs (`frequency`, `interval`, `byDay`, `byMonth`, `count`, `until`, `timezone`). Instance materialization happens in service layer using rules; persisted occurrences reference source task ID. *Alternative*: Persist every occurrence would bloat storage.
- **Dependency representation**: Store adjacency as DTO edges `{ id, taskId, dependsOnId, type }` with `type` as `EDependencyType`. Build in-memory adjacency maps for validation (cycle detection, deletion impact). *Alternative*: Store dependency lists on tasks; edges are harder to validate for cycles.
- **Versioning and concurrency**: Each collection payload holds `version` (monotonic int) and `updatedAt`. Unit of Work reads current version and requires equality before commit; on mismatch, surface a concurrency error and reload. *Alternative*: Last-write-wins without versioning risks data loss across tabs.
- **Validation**: On commit, verify foreign keys exist (task→category/priority, dependencies, reminders, attachments). Reject commits with broken references. *Alternative*: Best-effort writes would allow orphaned data.
- **Statistics computation**: Derive stats (counts by status/priority/category, overdue, upcoming, dependency-blocked) on demand from in-memory collections; no persisted stats cache. *Alternative*: Persisted stats risk staleness and add write complexity.
- **Generic utilities (planned)**: `createCrudRepository<TDto, TId>()`, `mapDtoToDomain<TDto, TDomain>()/mapDomainToDto<TDomain, TDto>()`, `buildIndex<T, K extends keyof T>()` for keyed lookup, plus `applyChangeset<T>()` for batch mutations. *Rationale*: Reuse across repositories and reduce boilerplate while keeping types strict.
- **Migration posture**: Keep repository interfaces storage-agnostic; localStorage gateway isolated. *Rationale*: Eases swap to IndexedDB or remote API later.

## Risks / Trade-offs

- **LocalStorage size and sync** → Limited capacity and synchronous I/O may impact large datasets; mitigate by normalized DTOs, minimal duplication, and batching writes.
- **Multi-tab edits** → Even with version checks, concurrent edits can conflict; mitigate by surfacing concurrency errors and offering reload prompts.
- **Cascade complexity** → Incorrect cascade logic could delete too much or too little; mitigate with exhaustive unit tests for dependency/recurrence/subtask graphs and dry-run validation.
- **Index rebuild cost** → Rehydration on app start re-parses all collections; acceptable given dataset size, mitigated by lazy loading and memoized indexes.
- **Cycle detection** → Dependency cycles must be rejected; implement cycle checks in validation; risk of O(n^2) in worst graphs but acceptable for small datasets.
- **Recurrence expansion drift** → Timezone and daylight shifts can misalign generated occurrences; mitigate by storing timezone in rules and using a consistent date library for expansion.

## Migration Plan

1) Review assignment1 storage shape and domain behaviors; extract relevant DTO patterns without modifying assignment1.
2) Define DTO types, mappers, and repository interfaces aligning with existing domain interfaces and naming rules (I*/E*).
3) Implement localStorage gateway (load/save per collection with version metadata) and generic utilities.
4) Implement repositories (tasks, categories, priorities, recurrence, dependencies, comments, attachments, reminders) using the gateway and mappers.
5) Implement Unit of Work with change-set aggregation, validation (FKs, cycles), cascade delete resolution, and versioned commit.
6) Add search/filter/sort services over in-memory indexes; add statistics derivations.
7) Write tests for repositories, UOW commit paths, cascades, search/filter/sort, and validation failures.

## Open Questions

- What is the expected max dataset size (tasks, comments, attachments) to size indexes and evaluate localStorage limits?
- Are multi-tab scenarios common, and should we add proactive change notifications (storage events) to trigger reloads?
- Any need for soft-delete vs hard-delete semantics for tasks and dependents?
- Should recurrence expansion be persisted for upcoming instances or always computed on the fly?
- Do attachments store binary references (e.g., data URLs) or external links only, given storage limits?
