# 20 - Architecture Overview

## Architecture Style

**Clean Architecture + Vertical Slice Organization**

We use Clean Architecture principles (dependency direction inward) with feature-based organization within layers.

---

## Project Structure

```
Conference and Event Venue Platform SaaS/
├── src/
│   ├── VenuePlatform.Web/              # Presentation Layer (Web)
│   ├── VenuePlatform.BLL/              # Business Logic Layer
│   │   ├── Domain/                     # Domain entities, value objects, events
│   │   └── Application/                # Use cases, CQRS handlers, DTOs
│   ├── VenuePlatform.DAL/              # Data Access Layer
│   │   ├── Data/                       # DbContext, migrations, configurations
│   │   ├── Identity/                   # Identity stores and services
│   │   └── Repositories/               # Repository implementations
│   └── VenuePlatform.Contracts/        # Shared contracts and interfaces
├── tests/
│   ├── VenuePlatform.UnitTests/
│   ├── VenuePlatform.IntegrationTests/
│   └── VenuePlatform.ArchitectureTests/
└── docs/
```

---

## Layer Responsibilities

### Web Layer (VenuePlatform.Web)

**Responsibilities:**
- HTTP request handling
- MVC Controllers / Razor Pages
- ViewModels and view logic
- Static assets
- Middleware configuration
- Dependency injection setup

**Dependencies:**
- References: BLL.Application, Contracts
- Does NOT reference: DAL directly

### Business Logic Layer (VenuePlatform.BLL)

#### Domain (VenuePlatform.BLL.Domain)

**Responsibilities:**
- Domain entities with behavior
- Value objects
- Domain events
- Business rules and invariants
- Repository interfaces
- Domain services

**Key Principles:**
- No dependencies on other projects
- Rich domain model (not anemic)
- Encapsulation of business logic

#### Application (VenuePlatform.BLL.Application)

**Responsibilities:**
- CQRS commands and queries
- DTOs (Data Transfer Objects)
- Validation logic
- Application services
- MediatR handlers
- Cross-cutting behaviors (audit, tenant filter)

**Key Principles:**
- Depends only on Domain
- Orchestrates use cases
- No business logic, only coordination

### Data Access Layer (VenuePlatform.DAL)

**Responsibilities:**
- Entity Framework DbContext
- Database migrations
- Repository implementations
- Identity storage
- Query filters (multi-tenancy, soft delete)
- External service integrations

**Key Principles:**
- Implements interfaces defined in BLL
- Contains all persistence concerns
- Global query filters for tenant isolation

### Contracts (VenuePlatform.Contracts)

**Responsibilities:**
- API request/response contracts
- Shared interfaces
- Constants and enums used across layers
- DTOs for API boundary

**Key Principles:**
- No dependencies
- Used for API versioning

---

## Dependency Flow

```
Web → BLL.Application → BLL.Domain
  ↓        ↓
Contracts ← DAL
```

**Dependency Rule:** Dependencies point inward. Outer layers depend on inner layers via interfaces.

### Enforced Dependency Rules

1. **BLL must NOT depend on DAL**
   - BLL.Domain has zero external project references
   - BLL.Application only references BLL.Domain and Contracts
   - Repository interfaces defined in BLL.Domain

2. **Interfaces in BLL, Implementations in DAL**
   - All service interfaces (`IFeatureChecker`, `ITenantContext`, etc.) defined in BLL.Application
   - All implementations live in DAL or Web projects
   - Dependency injection wires implementations at composition root

3. **Web references BLL + Contracts only**
   - Web project does NOT reference DAL directly
   - Data access only through BLL.Application services/commands
   - DbContext lifetime managed in DAL, injected via interface

### Example - Correct Dependency Direction

```csharp
// BLL.Application/Interfaces/ICurrentUser.cs (in BLL)
public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsSystemAdmin { get; }
}

// Web/Services/CurrentUser.cs (implementation in Web)
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;
    // Implementation uses HttpContext
}

// DAL/Services/FeatureChecker.cs (implementation in DAL)
public class FeatureChecker : IFeatureChecker
{
    private readonly ApplicationDbContext _dbContext;  // DAL can reference DbContext
    private readonly ICurrentUser _currentUser;        // Via interface from BLL
    // Implementation...
}
```

---

## Feature Organization (Vertical Slices)

Within each layer, organize by feature:

```
VenuePlatform.BLL.Application/
├── Features/
│   ├── Bookings/
│   │   ├── Commands/
│   │   │   ├── CreateBooking/
│   │   │   ├── UpdateBooking/
│   │   │   └── CancelBooking/
│   │   ├── Queries/
│   │   │   ├── GetBookingById/
│   │   │   ├── SearchBookings/
│   │   │   └── GetBookingCalendar/
│   │   └── Validators/
│   ├── Spaces/
│   ├── Catering/
│   ├── Equipment/
│   ├── Clients/
│   ├── Invoicing/
│   └── TenantManagement/
```

---

## Cross-Cutting Concerns

### Multi-Tenancy

- **Tenant Resolution:** From route `{companySlug}`
- **Tenant Context:** Stored in `ITenantContext` service
- **Query Filter:** Global EF filter `e.CompanyId == currentTenantId`
- **Isolation:** All business entities have `CompanyId` property

### Audit Trail

- **Entities:** `IAuditableEntity` interface
- **Properties:** `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Implementation:** MediatR pipeline behavior

### Soft Deletes

- **Interface:** `ISoftDelete`
- **Property:** `IsDeleted`, `DeletedAt`, `DeletedBy`
- **Query Filter:** Global filter excludes deleted records
- **Override:** `IgnoreQueryFilters()` for admin queries

### Feature Gating

- **Attribute:** `[RequireFeature(FeatureType)]`
- **Check:** `IFeatureChecker` service
- **Location:** Applied at controller action or handler level

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 |
| Database | SQLite (dev), PostgreSQL (prod) |
| Identity | ASP.NET Core Identity |
| Mediator | MediatR |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| API Documentation | Swagger/OpenAPI |
| Testing | xUnit, Moq, FluentAssertions |

---

## Architectural Patterns

1. **CQRS** - Separate commands (write) and queries (read)
2. **Repository Pattern** - Abstract data access
3. **Unit of Work** - Transaction boundary per request
4. **Domain Events** - Loose coupling between aggregates
5. **Pipeline Behaviors** - Cross-cutting concerns (logging, validation, audit)
6. **Options Pattern** - Configuration management
