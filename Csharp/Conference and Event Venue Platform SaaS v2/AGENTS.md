# AGENTS.md - AI Agent Guidance for Conference & Event Venue Platform

## Project Overview

This is a Conference & Event Venue Platform SaaS application for venues that manage meeting rooms, conference halls, equipment, catering, and bookings in one system.

## Technology Stack

- **Backend**: ASP.NET Core MVC (.NET 8)
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
| CompanyEmployee | Handles bookings and day-of event coordination |
| CompanyManager | Manages space configuration, pricing, room setup |
| CompanyAdmin | Manages venue, partners, business settings |

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
2. **Use EF Core conventions** - Follow the patterns in existing migrations and DbContext
3. **Maintain localization** - Add resource keys to `.resx` files for user-facing strings
4. **Write tests** - Add unit tests for new business logic in `WebApp.Tests/`
5. **Consider migrations** - If adding new entities, create a migration: `dotnet ef migrations add <Name>`
6. **Respect SOLID** - Keep services focused and dependencies injected
7. **Apply Clean Code** - Write readable code with meaningful names, small functions
8. **Apply KISS** - Prefer simple solutions over complex ones
9. **Apply DRY** - Extract duplicated logic into shared methods/services
10. **Apply YAGNI** - Don't add functionality until it's explicitly needed
11. **Apply Onion Architecture** - Core domain should not depend on infrastructure concerns
