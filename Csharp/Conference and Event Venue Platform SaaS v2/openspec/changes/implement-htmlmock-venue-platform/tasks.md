## 1. Domain Foundation

- [ ] 1.1 Define core domain entities and enums for companies, venues, venue memberships/access requests, spaces, layouts, bookings, catering orders, and equipment allocation in `App.Domain`
- [ ] 1.2 Add value objects or helper abstractions for status, pricing, capacity, and scheduling concepts needed by the mock flows
- [ ] 1.3 Replace the starter role assumptions with product roles so `User`, `CompanyEmployee`, and `CompanyManager` can be enforced consistently, and define the platform-admin responsibility if venue review remains cross-venue
- [ ] 1.4 Define the active-venue context model so multi-venue employees and managers can select the venue that scopes dashboards, bookings, catering, and management screens

## 2. Persistence Layer

- [ ] 2.1 Add `DbSet` registrations and entity configuration for the new venue domain inside `App.DAL.EF`
- [ ] 2.2 Create EF Core migrations for the initial venue operations schema and verify it remains compatible with the existing Identity schema
- [ ] 2.3 Add or update seed data so public discovery, request review, employee dashboard, and admin configuration screens have realistic development data

## 3. Business Logic Layer

- [ ] 3.1 Implement public discovery and venue request services for landing/browse data and venue access request submission
- [ ] 3.2 Implement employee workspace services that aggregate bookings, event coordination details, and catering order summaries
- [ ] 3.3 Implement venue membership and access-control services for rights assignment, blocked states, and active-venue selection
- [ ] 3.4 Implement venue admin services for dashboard metrics, request triage actions, and spaces/layout configuration workflows
- [ ] 3.5 Add DTOs or mapping models needed to move data cleanly between DAL/BLL and MVC views or API endpoints

## 4. Public And Identity UI

- [ ] 4.1 Replace the scaffolded home page with the branded public landing experience derived from `HtmlMock/landingpage.html`
- [ ] 4.2 Implement browse venues and request venue pages with MVC controllers, view models, validation, and localized user-facing strings
- [ ] 4.3 Reskin the login and registration flows to match the mock design while preserving ASP.NET Core Identity behavior
- [ ] 4.4 Extract shared public-facing CSS variables, typography, and reusable partials so the mock visual language is not duplicated page by page

## 5. Employee Workspace UI

- [ ] 5.1 Add employee-area routing, authorization, and navigation for dashboard, bookings, coordination, and catering workflows
- [ ] 5.2 Implement employee dashboard venue selection and bookings views using BLL aggregation models rather than direct EF queries
- [ ] 5.3 Implement the employee coordination and catering order views, including edit constraints such as catering lock timing
- [ ] 5.4 Extract shared employee-facing layout and component partials from the mock screens to keep the workspace maintainable

## 6. Manager And Admin UI

- [ ] 6.1 Add company-manager/platform-admin routing, authorization, and navigation for venue dashboard, space configuration, and venue request review
- [ ] 6.2 Implement the admin venue requests workflow for listing requests, viewing request details, updating review status, and assigning venue rights where allowed
- [ ] 6.3 Implement the company management dashboard and spaces/layout configuration views with forms and validation rules backed by BLL services
- [ ] 6.4 Extract shared manager/admin styling and panel components from the mock screens to avoid duplicating layout code

## 7. Verification

- [ ] 7.1 Add unit tests for request submission, request review, rights assignment, active-venue selection, booking aggregation, catering edit rules, and space configuration logic in `WebApp.Tests`
- [ ] 7.2 Add integration tests for the public entry flow, venue request workflow, employee workspace authorization, venue-selection behavior, and company-manager/platform-admin authorization boundaries
- [ ] 7.3 Run the full test suite and fix regressions introduced by the new schema, routing, and role-based workflows
