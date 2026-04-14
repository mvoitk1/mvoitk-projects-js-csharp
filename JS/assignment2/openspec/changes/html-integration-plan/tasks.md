## 1. Scope alignment

- [ ] 1.1 Audit `src/js/app.js` DOM dependencies and list required ids/classes.
- [ ] 1.2 Limit HTML integration scope to existing runnable flows (dashboard, tasks, calendar, modal CRUD).
- [ ] 1.3 Document deferred features that require separate DAL/UI change.

## 2. HTML shell implementation

- [ ] 2.1 Replace placeholder `index.html` with semantic app layout (header, sidebar, main views, modal).
- [ ] 2.2 Add all required element ids/classes used by `App.cacheElements()` and listeners.
- [ ] 2.3 Add script tags in dependency-safe order for `src/js/*.js`.

## 3. Baseline UX/CSS

- [ ] 3.1 Implement responsive layout for desktop and mobile.
- [ ] 3.2 Add accessible form labels, focus styles, and keyboard-friendly controls.
- [ ] 3.3 Style task cards, badges, filters, calendar grid, and modal for clear information hierarchy.

## 4. Runtime verification

- [ ] 4.1 Smoke test app boot (no missing element/runtime errors in browser console).
- [ ] 4.2 Verify core workflows: create, edit, delete, search, status filtering, calendar navigation.
- [ ] 4.3 Verify persistence roundtrip using localStorage.

## 5. Clean-code hardening

- [ ] 5.1 Remove inline event handlers from HTML; keep behavior in JS modules.
- [ ] 5.2 Keep CSS and structure modular and readable (no duplicated selector blocks).
- [ ] 5.3 Ensure naming remains consistent with existing module expectations.

## 6. Follow-up handoff

- [ ] 6.1 Add note for Phase 2 adapter-based migration to TypeScript DAL/UOW.
- [ ] 6.2 Capture known gaps and technical debt in the change notes.
