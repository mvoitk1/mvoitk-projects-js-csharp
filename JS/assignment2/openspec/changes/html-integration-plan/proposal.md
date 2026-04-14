## Why

The project already contains substantial browser UI logic in `src/js/app.js` plus task logic/validation/storage modules, but `index.html` and `src/main.ts` are placeholders. Without a concrete HTML shell and integration plan, the application cannot run as an actual UI.

The current plan is also over-scoped versus what is implemented. It lists entities and views that are not yet wired end-to-end in the running UI flow. We need a KISS-first integration plan that delivers a usable interface first, then iterates.

## What Changes

- Create a production-ready `index.html` shell that matches the DOM contract expected by `src/js/app.js`.
- Integrate existing JS modules (`utils`, `validator`, `storage`, `taskManager`, `formatters`, `commands`, `app`) in dependency order.
- Deliver the baseline UI/UX flows already implemented in code: task CRUD, status filters, search, dashboard cards, calendar view, and modal editing.
- Align scope with existing runtime behavior; defer advanced entities and collaboration features until their repositories/services are truly wired.
- Preserve clean boundaries so future TypeScript DAL integration can replace storage/task modules without redesigning the UI.

### Essential Features for HTML Integration

#### UI Components
- Task creation, viewing, editing, and deletion using the existing modal flow
- Dashboard with summary counters, recent tasks, and upcoming deadlines
- Task list with status filters and free-text search
- Calendar month view for due-date visibility
- Responsive navigation between dashboard, tasks, and calendar

#### Data Access Layer Integration
- Phase 1: use existing `src/js/storage.js` + `src/js/taskManager.js` for persistence and interaction
- Phase 2 (follow-up change): adapt UI boundary to the TypeScript DAL repository/UOW layer
- Keep a thin UI adapter boundary to avoid rewriting view code during migration

#### Client-Side JavaScript Logic
- Reuse current dynamic interactions in `App`/`TaskManager`
- Keep immediate UI refreshes after create/update/delete
- Preserve existing keyboard shortcuts and validation behavior

#### TypeScript Type Safety
- Do not block HTML delivery on full UI TS migration
- Ensure DOM ids/classes and data shape stay compatible with eventual typed adapter inputs

#### Responsive Design
- Mobile-friendly layout that adapts to different screen sizes
- Touch-friendly controls for mobile devices
- Responsive layout for dashboard cards, task list, and modal
- Accessible form controls and visible focus states

#### User Journeys and Workflows
- Task creation: validate -> persist -> refresh dashboard/list/calendar
- Task editing: load -> edit -> save -> refresh
- Search/filter: update state -> rerender task list
- Calendar: change month -> render due tasks -> open task modal from day entry

## Capabilities

### New Capabilities
- `html-ui-integration`: Deliver a functioning HTML/CSS shell for the existing UI logic.
- `ui-runtime-wiring`: Wire JS modules and DOM contract so app initialization succeeds in browser.
- `ux-baseline`: Provide consistent, responsive baseline UX for dashboard/tasks/calendar flows.

### Modified Capabilities
- Refine previous scope to explicitly prioritize baseline task workflows over non-implemented advanced entities.

## Impact

- Updates `index.html` from placeholder to full app shell
- Adds/updates UI assets (styles and structure) required by `src/js/app.js`
- Keeps existing JS modules intact with minimal adapter glue
- Defers full TypeScript DAL wiring to a dedicated follow-up change
