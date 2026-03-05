## Context

- `src/js/app.js` expects a concrete DOM structure (specific ids/classes for header, navigation, badges, views, task containers, calendar, and modal).
- `index.html` currently contains only a console statement and does not provide any mount points.
- Existing runtime modules are plain browser JS globals, not ES module imports.
- TypeScript DAL exists in `src/dal/*` but is not currently connected to the running UI flow.

## Goals / Non-Goals

**Goals:**
- Deliver a working HTML/CSS shell that allows the current JS app to run.
- Keep UI structure simple and predictable (KISS).
- Keep module boundaries clean so DAL migration can happen later.
- Establish a baseline UX that is responsive and accessible.

**Non-Goals:**
- No full rewrite of `src/js/*` to TypeScript in this change.
- No implementation of entities/features not already present in the active UI flows.
- No backend/network features.

## Decisions

- **Use existing JS UI contract as source of truth**: build `index.html` to match ids/classes required by `App.cacheElements()` and event bindings.  
  Rationale: fastest path to a running app with lowest risk.

- **Keep script loading explicit and ordered**: include JS files in dependency order so globals (`Utils`, `Validator`, `Storage`, `TaskManager`, `Formatters`, `Commands`, `App`) are defined when needed.  
  Rationale: avoids brittle runtime errors.

- **Single-page shell with three views** (`dashboard`, `tasks`, `calendar`) and one modal.  
  Rationale: matches current behavior and keeps UX cohesive.

- **Baseline responsive CSS only**: prioritize readability, spacing, focus states, and mobile usability over heavy visual complexity.  
  Rationale: KISS and clean maintainability.

- **Phase-based integration**: first run against existing JS storage/task manager; later introduce a typed adapter to DAL repositories/UOW.  
  Rationale: avoids coupling UI completion to larger migration scope.

## UI Structure (Contract)

- Header: `#header-title`, `#search-input`, `#add-task-btn`
- Sidebar/nav:
  - `.nav-item[data-view="dashboard|tasks|calendar"]`
  - `.filter-nav[data-filter]`
  - badges: `#total-badge`, `#pending-badge`, `#progress-badge`, `#completed-badge`
- Views:
  - `#dashboard-view.view`
  - `#tasks-view.view`
  - `#calendar-view.view`
- Dashboard containers: `#recent-tasks`, `#upcoming-tasks`, stats ids `#stat-total`, `#stat-pending`, `#stat-progress`, `#stat-completed`
- Tasks view container: `#all-tasks`, status filter buttons `.filter-btn[data-status]`
- Calendar: `#calendar-month`, `#calendar-grid`, `#prev-month`, `#next-month`, `#today-btn`
- Modal/form:
  - `#task-modal`, `#task-form`, `#modal-title`
  - fields: `#task-id`, `#task-title`, `#task-description`, `#task-status`, `#task-priority`, `#task-due-date`
  - tags: `#tags-container`, `#tags-input`
  - actions: `#save-task-btn`, `#delete-task-btn`, `#cancel-btn`, `#modal-close`

## Risks / Trade-offs

- **Risk: JS global ordering bugs** -> mitigate with deterministic script tag order and quick smoke test.
- **Risk: UI/Type mismatch with DAL** -> mitigate by isolating storage/task access behind adapter boundary later.
- **Risk: scope creep** -> mitigate by limiting this change to existing task UX flows.

## Migration Follow-up

1) Introduce a UI data adapter interface consumed by `App`.  
2) Back adapter with TypeScript DAL repositories/UOW.  
3) Gradually retire legacy JS storage/task manager modules once behavior parity is verified.
