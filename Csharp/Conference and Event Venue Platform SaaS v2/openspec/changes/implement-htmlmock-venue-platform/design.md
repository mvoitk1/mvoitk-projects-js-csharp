## Context

The current solution already has ASP.NET Core MVC, Identity, Swagger, localization plumbing, and an EF Core PostgreSQL context, but it does not yet model the venue business domain shown in `HtmlMock/`. The mock set covers three visible experience bands: public entry and marketing, employee booking operations, and admin-oriented venue administration. The implementation must go beyond the mocks by introducing an explicit company-manager role for tenant-side management, while keeping cross-venue onboarding and rights review separate if those workflows stay platform-admin responsibilities. Because the current codebase is mostly scaffolded, the main design challenge is introducing a minimal but coherent domain model and application structure that can support those screens without overbuilding features that the mocks do not require.

## Goals / Non-Goals

**Goals:**
- Define a first-pass domain model that supports the mock pages end to end.
- Preserve the existing N-tier structure by placing business rules in `App.BLL`, persistence in `App.DAL.EF`, and presentation logic in `WebApp`.
- Introduce role-aware MVC areas and shared styling assets that can implement the mock UI direction without duplicating layout code.
- Sequence the work so foundational entities and services land before screen-level implementation.
- Keep the initial scope aligned to the mock set and avoid speculative modules.

**Non-Goals:**
- Full premium multi-venue feature set beyond what is required by the current mock screens.
- Comprehensive billing and invoicing workflows beyond the summary information visible in the mocks.
- Real-time collaboration, notifications, external catering integrations, or advanced reporting.
- A separate SPA frontend; the implementation remains server-rendered MVC with the existing stack.

## Decisions

### Decision: Implement the mock set as four capability slices rather than page-by-page tickets
Rationale: The mock files cluster naturally into public, onboarding, employee, and admin flows. Grouping by capability keeps data modeling, service boundaries, and tests coherent.
Alternatives considered:
- One ticket per page: rejected because it would duplicate domain and service work across screens.
- One monolithic “build the product” change: rejected because it is too broad to validate incrementally.

### Decision: Add a minimal venue operations domain centered on Venue, Space, Booking, CateringOrder, EquipmentAllocation, and VenueAccessRequest
Rationale: These concepts cover the data shown in the mock screens without forcing early implementation of recurring templates, split billing, or partner integrations.
Alternatives considered:
- Model every concept from the README immediately: rejected as too much upfront schema design.
- Keep data in view models only until UI is stable: rejected because the project already uses EF Core and layered architecture.

### Decision: Use role-based MVC areas and shared layout/theme partials instead of separate apps
Rationale: The project already has MVC Areas and Identity. Public, employee, and admin experiences can share composition primitives while still having distinct navigation and visual treatments.
Alternatives considered:
- Separate frontend projects per role: rejected as unnecessary complexity.
- Single controller namespace without areas: rejected because the mock set implies clear role segmentation.

### Decision: Treat the starter application's current Identity roles and admin folders as replaceable scaffolding
Rationale: The repository is a base project, not a role model to preserve. The target product needs at least `User`, `CompanyEmployee`, `CompanyManager`, plus a clear platform-level administrative responsibility for venue review and access control if those workflows remain cross-venue.
Alternatives considered:
- Keep the starter roles and map everything onto them: rejected because it would force the business model to conform to scaffold terminology.
- Limit the first slice to only the three roles visible in the mocks: rejected because the project requirements still need a tenant-side management role plus a clear answer for cross-venue review authority.

### Decision: Model venue membership and active venue context explicitly
Rationale: The approved direction allows employees and managers to belong to multiple venues, and several mock screens imply a current venue that scopes dashboards, bookings, catering, and configuration. This requires explicit membership data plus an active-venue selection mechanism rather than relying on global roles alone.
Alternatives considered:
- Single-venue foreign key on user: rejected because it conflicts with the agreed multi-venue first schema.
- Infer active venue only from the last opened booking or route: rejected because it is too implicit for authorization and dashboard aggregation.

### Decision: Translate mock CSS into reusable design tokens and shared components inside `WebApp`
Rationale: The mock pages already reveal repeated color systems, typography pairings, cards, sidebars, and metric panels. Consolidating these into shared partials and CSS variables reduces duplication and keeps the implementation maintainable.
Alternatives considered:
- Copy each HTML file directly into a Razor view: rejected because it would create duplication and slow later changes.
- Replace the mock aesthetic with Bootstrap defaults: rejected because it would lose the intended product direction.

### Decision: Build aggregation-oriented BLL services for dashboards and operational lists
Rationale: Pages such as employee dashboard, my bookings, venue admin dashboard, and venue request review depend on composed data rather than raw entities. Dedicated query services keep controllers thin and avoid leaking EF queries into views.
Alternatives considered:
- Query directly from controllers: rejected because it violates separation of concerns.
- Build only CRUD services: rejected because the mock screens are workflow-oriented, not CRUD-only.

## Risks / Trade-offs

- [Scope growth from README capabilities] -> Mitigation: constrain the first implementation to data and behaviors evidenced by the mock screens and proposal specs.
- [Large first migration] -> Mitigation: split schema work into coherent migrations by domain slice where possible and validate each slice with tests.
- [UI duplication across roles] -> Mitigation: extract shared layout primitives, card components, and CSS variables before building later pages.
- [Authorization gaps while adding new areas] -> Mitigation: define role access rules early and cover them with integration tests.
- [Role ambiguity between company manager and platform admin responsibilities] -> Mitigation: separate tenant-scoped management from cross-venue approval/review workflows in the service and routing design before implementation starts.
- [Mock-to-data mismatch] -> Mitigation: use dashboard/query DTOs that can shape screen data without overloading entities.

## Migration Plan

- Add core domain entities and EF mappings in a sequence that keeps the application buildable after each slice.
- Replace or retire starter role assumptions as the venue-specific role model lands, while only deleting obsolete scaffold files or areas after explicit review.
- Create and apply migrations for the venue domain after the entity model is stabilized for the first slice.
- Seed enough local data to exercise public discovery, request review, employee bookings, and admin configuration views.
- Introduce new controllers, areas, and views behind normal MVC routing; existing scaffold routes remain available during implementation.
- Rollback path: remove the latest migration and disable the new routes if a slice is incomplete, since no external integration is required for the initial rollout.

## Open Questions

- Should initial registration create only an Identity account, or also bootstrap a company profile when the user is a venue operator?
  - Answer: When a account is registered, it will be a standard Identity account. The company profile and venue access request will be created in a subsequent step when the user submits a venue access request form.
- Are employees and managers always tied to exactly one venue in the first release, or must the Premium multi-venue concept appear in the initial schema?
  - Answer: Employees and managers can be tied to multiple venues in the initial schema and they can select which venue they are operating in when they log in. This allows the multi-venue concept to be present without forcing complex features like cross-venue scheduling or billing.
- Does catering order management need inventory/partner enforcement in the first slice, or only editable order summaries and deadlines?
  - Answer: The catering can be hardcoded form a few partner menus for the initial implementation, and the BLL can enforce edit deadlines without inventory constraints.
- Which of the existing identity roles map directly to `CompanyEmployee`, `CompanyManager`, and `CompanyAdmin`, and are any additional system roles required?
  - Answer: `CompanyManager` and `CompanyAdmin` are the same tenant-side role in this project and should be represented as a single product role. The starter project's current roles are scaffolding and can be changed. The target implementation should define explicit product roles for `User`, `CompanyEmployee`, and `CompanyManager`. If venue request review and rights assignment remain cross-venue workflows, a separate platform-admin responsibility should exist in the implementation even if the final naming changes.
