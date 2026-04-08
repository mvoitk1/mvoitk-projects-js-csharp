## Why

The repository currently contains only framework scaffolding, while `HtmlMock/` defines the actual product experience expected for the conference and venue SaaS. A proposal is needed now to translate those mock screens into implementable capabilities, data model changes, and layered work items before coding starts.

## What Changes

- Introduce the first product slice that maps the `HtmlMock` pages into concrete MVC capabilities for public users, company employees, company managers, and platform admins where operational review requires it.
- Define the missing venue domain required by the mock flows, including venues, spaces, bookings, catering requests, equipment allocation, and venue access requests.
- Establish role-aware application areas and navigation so public pages, employee workspace pages, company management pages, and platform admin review pages can coexist in the existing MVC application.
- Specify the service, persistence, and test work needed to move from a scaffolded Identity app to a functional venue operations platform.
- Defer non-mock features such as advanced invoicing, recurring-event automation, and premium multi-venue analytics unless needed to support the documented screens.

## Capabilities

### New Capabilities
- `public-venue-entry`: Marketing, authentication, and venue discovery flows based on the landing, login, registration, and browse venues mock pages.
- `company-venue-requesting`: Venue onboarding request submission and admin review workflows based on the request venue and admin venue requests mock pages.
- `employee-booking-workspace`: Booking dashboard, booking list, employee day-of coordination, and catering order management based on the employee-facing mock pages.
- `venue-admin-configuration`: Venue operations command center and spaces/layout configuration workflows for company managers, and platform admins where the workflow is cross-venue.

### Modified Capabilities
- None.

## Impact

- `App.Domain`: new entities, enums, and value objects for venues, companies, venue memberships, spaces, bookings, booking services, catering, and access requests.
- `App.DAL.EF`: `AppDbContext` updates, entity configuration, and new EF Core migrations.
- `App.BLL`: new services for discovery, request intake, dashboard aggregation, booking management, and venue configuration.
- `App.DTO`: view/API DTOs for role-specific screens and forms.
- `WebApp`: new MVC controllers, areas, view models, localized views, navigation updates, and static assets extracted from the mock designs.
- `WebApp.Tests`: unit coverage for business rules and integration coverage for the key role-based flows.
