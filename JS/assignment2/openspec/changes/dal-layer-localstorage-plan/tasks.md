## 1. Foundations and inputs

- [x] 1.1 Review assignment1 storage/domain shapes (read-only) to align DTOs and mappings
- [x] 1.2 Confirm proposal, specs, and design scope for DAL/localStorage, UOW, cascades, search/sort/stats
- [x] 1.3 Define TypeScript types/interfaces/enums for DAL DTOs, repositories, and UOW (I*/E* naming)

## 2. DTOs, mapping, and utilities

- [x] 2.1 Define DTO schemas for tasks, categories, priorities, dependencies, recurrence rules, comments, attachments, reminders, checklist items
- [x] 2.2 Implement domain↔DTO mappers with validation for required fields and FK presence
- [x] 2.3 Implement generic utilities: createCrudRepository<TDto,TId>, mapDtoToDomain/mapDomainToDto, buildIndex/applyChangeset helpers

## 3. localStorage gateway

- [x] 3.1 Implement namespaced envelope load/save with versioning and updatedAt metadata
- [x] 3.2 Add envelope validation, corruption quarantine, and initialization of missing collections
- [x] 3.3 Implement concurrency/version check and error reporting for mismatches

## 4. Repositories

- [x] 4.1 Implement tasks repository with CRUD, FK checks (category, priority), and checklist/subtask handling
- [x] 4.2 Implement categories and priorities repositories with CRUD and referential integrity guards
- [x] 4.3 Implement dependencies repository with edge CRUD and cycle detection hooks
- [x] 4.4 Implement recurrence repository for rule storage and retrieval
- [x] 4.5 Implement comments, attachments, reminders repositories with task FK enforcement

## 5. Unit of Work and cascades

- [x] 5.1 Implement UOW change-set aggregation across repositories
- [x] 5.2 Implement FK validation and cycle detection during UOW commit
- [x] 5.3 Implement cascade delete resolution (tasks → subtasks/checklists, dependencies, recurrence instances, comments, attachments, reminders)
- [x] 5.4 Implement atomic batch commit via localStorage gateway with rollback on validation failure

## 6. Search, filter, sort, and stats

- [x] 6.1 Build in-memory indexes over loaded DTOs (status, priority, category, tag/label, due buckets, recurrence presence, dependency-blocked)
- [x] 6.2 Implement query API for filters and multi-field sorts (dueDate, priority, createdAt, updatedAt)
- [x] 6.3 Implement dependency-blocked detection and filter
- [x] 6.4 Implement recurrence-aware date window queries (overdue/upcoming instances)
- [x] 6.5 Implement derived statistics (counts by status/priority/category, overdue, upcoming, blocked)

## 7. Validation, errors, and concurrency

- [x] 7.1 Add validation errors for missing/invalid foreign keys and required fields
- [x] 7.2 Add concurrency error paths surfaced from gateway/UOW version mismatches
- [x] 7.3 Add cycle detection errors for dependencies

## 8. Testing

- [x] 8.1 Unit tests for gateway envelope/versioning/quarantine behaviors
- [x] 8.2 Unit tests for repositories CRUD, FK enforcement, and mappers
- [x] 8.3 Unit tests for UOW commit paths, cascades, and rollback on validation failure
- [x] 8.4 Unit tests for search/filter/sort and dependency-blocked filters
- [x] 8.5 Unit tests for recurrence-aware queries and statistics derivation
