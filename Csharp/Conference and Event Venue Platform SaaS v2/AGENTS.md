# AGENTS.md - AI Agent Guidance for Conference & Event Venue Platform

## Project Overview

This is a Conference & Event Venue Platform SaaS application for venues that manage meeting rooms, conference halls, equipment, catering, and bookings in one system.

## Technology Stack

- **Backend**: ASP.NET Core MVC (.NET 10)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **API**: RESTful API with Swagger/OpenAPI
- **Testing**: xUnit with integration and unit tests

## Architecture

The project follows N-Tier layered architecture with clear separation of concerns:

```
VenuePlatform.sln
├── App.Domain/          # Domain entities, interfaces, and value objects
├── App.DAL.EF/          # Entity Framework context, migrations, seeding
├── App.BLL/             # Business logic services
├── App.DTO/             # Data transfer objects for API layer
├── App.Helpers/         # Utility helpers
├── App.Resources/       # Localization resources (.resx files)
├── Base.Resources/      # Base localization resources
├── WebApp/              # Main MVC application
└── WebApp.Tests/        # Unit and integration tests
```

## Software Engineering Principles

This project follows these principles:

- **SOLID**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Code**: Write readable, maintainable, and well-organized code
- **KISS (Keep It Simple, Stupid)**: Keep solutions simple and straightforward
- **DRY (Don't Repeat Yourself)**: Avoid code duplication, extract common logic into reusable components
- **YAGNI (You Aren't Gonna Need It)**: Only implement features that are explicitly needed
- **Onion Architecture**: Depend on core domain, not infrastructure - inner layers know nothing about outer layers

## Core Domain Concepts

### Spaces
- Configurable bookable spaces (boardrooms to auditoriums)
- Hourly rates and minimum booking durations
- Combinable rooms for flexible layouts
- Venue-specific availability and constraints

### Equipment
- Equipment packages (AV, recording, video conferencing)
- Separate pricing for equipment and add-ons
- Inventory tracking across simultaneous bookings
- Conflict detection for limited equipment

### Catering
- Configurable partner relationships
- Options from coffee service to full-day catering
- Dietary preferences and attendee-specific needs
- Configurable order lock times (default 72 hours)

### Bookings
- Event bookings across spaces, equipment, and catering
- Recurring event templates (monthly, quarterly)
- Client history and service notes
- Date adjustment when replicating templates

### Invoicing
- Itemized invoices
- Charges breakdown by space, equipment, catering, services
- Split billing across multiple cost centers

## User Roles

| Role | Responsibilities |
|------|------------------|
| User | Base authenticated account before venue membership or elevated access is assigned |
| CompanyEmployee | Handles bookings and day-of event coordination |
| CompanyManager | Manages space configuration, pricing, room setup |
| Admin | Platform-level administrative role for cross-venue review, onboarding decisions, and access control |

## Subscription Tiers

- **Standard**: Single-venue management
- **Premium**: Multi-venue management

## Database

- Database: PostgreSQL
- ORM: Entity Framework Core
- Migrations located in: `App.DAL.EF/Migrations/`
- Initial migration: `20251230183322_Initial`

## Getting Started

1. Ensure PostgreSQL is running
2. Update connection string in `WebApp/appsettings.json`
3. Run migrations: `dotnet ef database update`
4. Seed initial data: Built into application startup
5. Run the application: `dotnet run --project WebApp`

## API Endpoints

- Identity/Auth: `/api/Account/*` (login, register, refresh token)
- Swagger UI: `/swagger`

## Testing

- Run all tests: `dotnet test`
- Unit tests: `WebApp.Tests/Unit/`
- Integration tests: `WebApp.Tests/Integration/`

## Guidance for AI Agents

When working on this codebase:

1. **Follow the existing architecture** - Don't introduce new layers without justification
2. **Treat the current codebase as scaffolding** - Existing starter pages, roles, and placeholder assets are a base to build from, not product truth
3. **Use the implementation plan as the source of product truth** - Prefer the active OpenSpec change, design, tasks, and `HtmlMock/` flows over starter-template conventions
4. **Use this role model unless the user changes it** - `User`, `CompanyEmployee`, `CompanyManager`, and `Admin`
5. **Target framework is `net10.0`** - Do not downgrade to .NET 8 unless the user explicitly requests it
6. **Maintain localization** - Add resource keys to `.resx` files for user-facing strings
7. **Write tests** - Add unit tests for new business logic in `WebApp.Tests/`
8. **Consider migrations** - If adding new entities, create a migration: `dotnet ef migrations add <Name>`
9. **Respect SOLID** - Keep services focused and dependencies injected
10. **Apply Clean Code** - Write readable code with meaningful names, small functions
11. **Apply KISS** - Prefer simple solutions over complex ones
12. **Apply DRY** - Extract duplicated logic into shared methods/services
13. **Apply YAGNI** - Don't add functionality until it's explicitly needed
14. **Apply Onion Architecture** - Core domain should not depend on infrastructure concerns
15. **Log user prompts** - If the user gives you a prompt intended for future reuse or tracking, write that prompt into `AI/ai-prompts.md`
16. **Use EF Core conventions** - Follow the patterns in existing migrations and DbContext unless the implementation plan requires replacing scaffold conventions
17. **Assume some web assets are unrelated leftovers** - In particular, `WebApp/wwwroot/js/screens/*` appears to be demo/starter content and should only be kept if it is intentionally reused
18. **Prefer removal over preservation for unrelated scaffold/demo code** - Do not keep irrelevant starter artifacts just for continuity
