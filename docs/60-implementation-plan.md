# 60 - Implementation Plan

## Phase Overview

| Phase | Duration | Focus |
|-------|----------|-------|
| Phase 1 | 1-2 weeks | Foundation & Core Infrastructure |
| Phase 2 | 2-3 weeks | Identity, Tenant Management, Companies |
| Phase 3 | 3-4 weeks | Booking System (Core) |
| Phase 4 | 2-3 weeks | Equipment & Catering |
| Phase 5 | 2 weeks | Invoicing & Billing |
| Phase 6 | 1-2 weeks | UI Polish & Testing |

---

## Deliverable Format

Each deliverable includes:
- **Goal**: What will be accomplished
- **Files to create/modify**: Specific file paths
- **Dependencies**: What must be completed first
- **Acceptance criteria**: How to verify completion
- **Test scenario**: Manual verification steps

---

## Phase 1: Foundation & Core Infrastructure

### Deliverable 1.1: Project Structure Setup

**Goal**: Establish clean architecture project structure with clear naming

**Files to create:**
- `src/VenuePlatform.Web/VenuePlatform.Web.csproj`
- `src/VenuePlatform.BLL/VenuePlatform.BLL.csproj`
- `src/VenuePlatform.DAL/VenuePlatform.DAL.csproj`
- `src/VenuePlatform.Contracts/VenuePlatform.Contracts.csproj`
- `tests/VenuePlatform.UnitTests/VenuePlatform.UnitTests.csproj`
- `tests/VenuePlatform.IntegrationTests/VenuePlatform.IntegrationTests.csproj`
- `Conference and Event Venue Platform SaaS.sln` (update)

**Files to modify:**
- Delete: `WebApp/` project (migrate to new structure)
- Delete: `AppDomain/` project (migrate to new structure)
- Delete: `BaseDomain/` project (migrate to new structure)

**Dependencies:** None

**Acceptance criteria:**
- [ ] Solution builds successfully
- [ ] All 4 source projects created with correct dependencies
- [ ] Test projects reference appropriate source projects
- [ ] Old projects removed from solution

**Test scenario:**
```bash
dotnet build
# Verify no errors
dotnet test
# Verify test projects run (empty is OK)
```

---

### Deliverable 1.2: Base Domain Layer

**Goal**: Create domain primitives and base entities

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Common/BaseEntity.cs`
- `src/VenuePlatform.BLL/Domain/Common/AuditableEntity.cs`
- `src/VenuePlatform.BLL/Domain/Common/Interfaces.cs`
  - `IAuditableEntity`
  - `ISoftDelete`
  - `ITenantScoped`
- `src/VenuePlatform.BLL/Domain/Common/DomainEvent.cs`
- `src/VenuePlatform.BLL/VenuePlatform.BLL.csproj`

**Dependencies:** Deliverable 1.1

**Acceptance criteria:**
- [ ] All interfaces defined correctly
- [ ] `BaseEntity` implements all required interfaces
- [ ] Domain project has no external dependencies

**Test scenario:**
```csharp
// Verify BaseEntity structure
var entity = new TestEntity { CompanyId = Guid.NewGuid() };
Assert.NotEqual(Guid.Empty, entity.Id);
Assert.False(entity.IsDeleted);
```

---

### Deliverable 1.3: Contracts Layer

**Goal**: Define shared contracts and DTOs

**Files to create:**
- `src/VenuePlatform.Contracts/Common/Result.cs`
- `src/VenuePlatform.Contracts/Common/PagedResult.cs`
- `src/VenuePlatform.Contracts/Enums/SubscriptionTier.cs`
- `src/VenuePlatform.Contracts/Enums/TenantRole.cs`
- `src/VenuePlatform.Contracts/Enums/BookingStatus.cs`
- `src/VenuePlatform.Contracts/Enums/InvoiceStatus.cs`
- `src/VenuePlatform.Contracts/VenuePlatform.Contracts.csproj`

**Dependencies:** Deliverable 1.1

**Acceptance criteria:**
- [ ] Result pattern implemented with Success/Failure states
- [ ] All enums defined with proper values
- [ ] Contracts project has zero dependencies

**Test scenario:**
```csharp
var success = Result.Success();
var failure = Result.Failure("Error message");
var typed = Result<int>.Success(42);
```

---

### Deliverable 1.4: Data Access Layer - Infrastructure

**Goal**: Setup EF Core with multi-tenancy and soft delete

**Files to create:**
- `src/VenuePlatform.DAL/Data/ApplicationDbContext.cs`
- `src/VenuePlatform.DAL/Data/Configuration/CompanyConfiguration.cs`
- `src/VenuePlatform.DAL/DependencyInjection.cs`
- `src/VenuePlatform.DAL/VenuePlatform.DAL.csproj`

**Dependencies:** Deliverable 1.2, 1.3

**Acceptance criteria:**
- [ ] DbContext implements tenant query filter
- [ ] DbContext implements soft delete filter
- [ ] Dependency injection extension method exists
- [ ] SQLite configured for development

**Test scenario:**
```csharp
// Verify filters are applied
var companyId = Guid.NewGuid();
tenantContext.SetCompany(companyId, "test");
// Query should automatically filter by CompanyId
```

---

## Phase 2: Identity & Tenant Management

### Deliverable 2.1: Identity Entities

**Goal**: Setup custom Identity with multi-company support

**Files to create:**
- `src/VenuePlatform.DAL/Identity/AppUser.cs`
- `src/VenuePlatform.DAL/Identity/AppRole.cs`
- `src/VenuePlatform.DAL/Identity/AppUserRole.cs`
- `src/VenuePlatform.DAL/Identity/CompanyMembership.cs`
- `src/VenuePlatform.DAL/Data/Configuration/IdentityConfiguration.cs`

**Dependencies:** Deliverable 1.4

**Acceptance criteria:**
- [ ] AppUser extends IdentityUser<Guid>
- [ ] CompanyMembership links users to companies with role
- [ ] User can belong to multiple companies
- [ ] Migration created and applied

**Test scenario:**
```csharp
// Create user with company membership
var user = new AppUser { Email = "test@test.com", FirstName = "Test" };
var membership = new CompanyMembership { 
    UserId = user.Id, 
    CompanyId = companyId, 
    Role = TenantRole.CompanyOwner 
};
```

---

### Deliverable 2.2: Tenant Context & Middleware

**Goal**: Implement tenant resolution from route

**Files to create:**
- `src/VenuePlatform.BLL/Application/Common/Interfaces/ITenantContext.cs`
- `src/VenuePlatform.DAL/Services/TenantContext.cs`
- `src/VenuePlatform.Web/Middleware/TenantResolutionMiddleware.cs`
- `src/VenuePlatform.Web/DependencyInjection.cs`

**Dependencies:** Deliverable 2.1

**Acceptance criteria:**
- [ ] Middleware extracts companySlug from route
- [ ] TenantContext stores current CompanyId
- [ ] Scoped lifetime for TenantContext
- [ ] 404 if company slug not found

**Test scenario:**
```bash
# Request to /acme-corp/bookings
# Middleware should resolve "acme-corp" to CompanyId
# TenantContext.CurrentCompanyId should be set
```

---

### Deliverable 2.3: Company Management - Domain

**Goal**: Create Company aggregate root and related entities

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/Company.cs`
- `src/VenuePlatform.BLL/Domain/Entities/CompanySettings.cs`
- `src/VenuePlatform.BLL/Domain/Entities/Subscription.cs`
- `src/VenuePlatform.DAL/Data/Configuration/CompanyConfiguration.cs`

**Dependencies:** Deliverable 2.2

**Acceptance criteria:**
- [ ] Company entity with all required properties
- [ ] CompanySettings as owned entity
- [ ] Subscription entity for tier tracking
- [ ] Slug uniqueness constraint

**Test scenario:**
```csharp
var company = Company.Create("Acme Corp", "acme-corp", SubscriptionTier.Standard);
Assert.Equal("acme-corp", company.Slug);
Assert.NotNull(company.Settings);
```

---

### Deliverable 2.4: Self-Service Company Creation

**Goal**: Allow users to create new companies/tenants

**Files to create:**
- `src/VenuePlatform.BLL/Application/Features/Companies/CreateCompanyCommand.cs`
- `src/VenuePlatform.Web/Controllers/CompanyController.cs`
- `src/VenuePlatform.Web/Views/Company/Create.cshtml`
- `src/VenuePlatform.Web/Views/Company/Index.cshtml`

**Dependencies:** Deliverable 2.3

**Acceptance criteria:**
- [ ] Command validates slug uniqueness
- [ ] Creator automatically becomes CompanyOwner
- [ ] Default settings applied
- [ ] User sees company list after creation

**Test scenario:**
1. User clicks "Create Company"
2. Enters name "Test Corp", selects tier "Standard"
3. Slug auto-generated as "test-corp"
4. User redirected to /test-corp/bookings
5. User has CompanyOwner role

---

## Phase 3: Booking System (Core)

### Deliverable 3.1: Space Management

**Goal**: CRUD operations for spaces

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/Space.cs`
- `src/VenuePlatform.BLL/Domain/Entities/SpaceConfiguration.cs`
- `src/VenuePlatform.BLL/Application/Features/Spaces/CreateSpaceCommand.cs`
- `src/VenuePlatform.BLL/Application/Features/Spaces/GetSpacesQuery.cs`
- `src/VenuePlatform.Web/Controllers/SpacesController.cs`

**Dependencies:** Deliverable 2.4

**Acceptance criteria:**
- [ ] Space CRUD with tenant isolation
- [ ] Space configurations (combining rooms)
- [ ] Validation for capacity/rates

**Test scenario:**
```bash
POST /{companySlug}/spaces
{ name: "Conference Room A", capacity: 50 }

GET /{companySlug}/spaces
# Returns only spaces for this company
```

---

### Deliverable 3.2: Client Management

**Goal**: Client CRUD with contact management

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/Client.cs`
- `src/VenuePlatform.BLL/Domain/Entities/Contact.cs`
- `src/VenuePlatform.BLL/Application/Features/Clients/CreateClientCommand.cs`
- `src/VenuePlatform.BLL/Application/Features/Clients/GetClientsQuery.cs`
- `src/VenuePlatform.Web/Controllers/ClientsController.cs`

**Dependencies:** Deliverable 3.1

**Acceptance criteria:**
- [ ] Client with multiple contacts
- [ ] Primary contact designation
- [ ] History/notes tracking

**Test scenario:**
1. Create client "Microsoft Estonia"
2. Add contacts: "John Doe" (primary), "Jane Smith"
3. View client shows all contacts

---

### Deliverable 3.3: Basic Booking Creation

**Goal**: Create bookings with space assignment

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/Booking.cs`
- `src/VenuePlatform.BLL/Domain/Entities/BookingSpace.cs`
- `src/VenuePlatform.BLL/Application/Features/Bookings/CreateBookingCommand.cs`
- `src/VenuePlatform.BLL/Application/Features/Bookings/GetBookingQuery.cs`
- `src/VenuePlatform.Web/Controllers/BookingsController.cs`

**Dependencies:** Deliverable 3.2

**Acceptance criteria:**
- [ ] Booking with date/time range
- [ ] Multiple spaces per booking
- [ ] Client association
- [ ] Basic conflict detection (same space, overlapping times)

**Test scenario:**
```csharp
// Try to book same space at same time
var booking1 = CreateBooking(spaceA, "2026-03-10 09:00", "2026-03-10 12:00");
var booking2 = CreateBooking(spaceA, "2026-03-10 11:00", "2026-03-10 14:00");
// booking2 should fail with conflict error
```

---

### Deliverable 3.4: Booking Calendar View

**Goal**: Calendar interface for viewing bookings

**Files to create:**
- `src/VenuePlatform.BLL/Application/Features/Bookings/GetBookingCalendarQuery.cs`
- `src/VenuePlatform.Web/Views/Bookings/Calendar.cshtml`
- `src/VenuePlatform.Web/wwwroot/js/calendar.js`

**Dependencies:** Deliverable 3.3

**Acceptance criteria:**
- [ ] Monthly/weekly view
- [ ] Bookings displayed on calendar
- [ ] Click to view details
- [ ] Filter by space

**Test scenario:**
1. Navigate to /{companySlug}/bookings/calendar
2. See bookings for current month
3. Click booking to see details
4. Switch to space view to see availability

---

### Deliverable 3.5: Recurring Bookings

**Goal**: Template-based recurring booking creation

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/RecurringTemplate.cs`
- `src/VenuePlatform.BLL/Application/Features/Bookings/CreateRecurringTemplateCommand.cs`
- `src/VenuePlatform.BLL/Application/Features/Bookings/GenerateRecurringBookingsCommand.cs`
- `src/VenuePlatform.Web/Views/Bookings/CreateRecurring.cshtml`

**Dependencies:** Deliverable 3.4

**Acceptance criteria:**
- [ ] Weekly/monthly patterns
- [ ] Generate multiple bookings from template
- [ ] Feature-gated (Standard+)

**Test scenario:**
1. Create template: "Weekly Team Meeting", Mondays 10:00-11:00, 10 occurrences
2. System generates 10 bookings
3. Bookings linked to template

---

## Phase 4: Equipment & Catering

### Deliverable 4.1: Equipment Inventory

**Goal**: Equipment management with availability tracking

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/Equipment.cs`
- `src/VenuePlatform.BLL/Domain/Entities/EquipmentPackage.cs`
- `src/VenuePlatform.BLL/Domain/Entities/BookingEquipment.cs`
- `src/VenuePlatform.BLL/Application/Features/Equipment/CreateEquipmentCommand.cs`
- `src/VenuePlatform.Web/Controllers/EquipmentController.cs`

**Dependencies:** Deliverable 3.5

**Acceptance criteria:**
- [ ] Equipment with quantity tracking
- [ ] Equipment packages
- [ ] Add equipment to bookings
- [ ] Conflict detection for insufficient quantity

**Test scenario:**
```csharp
// 5 projectors available
var booking1 = AddEquipment(projector, 3); // OK
var booking2 = AddEquipment(projector, 3); // Fail, only 2 available
```

---

### Deliverable 4.2: Catering Options & Orders

**Goal**: Catering management with lead-time locking

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/CateringOption.cs`
- `src/VenuePlatform.BLL/Domain/Entities/CateringOrder.cs`
- `src/VenuePlatform.BLL/Domain/Entities/Attendee.cs`
- `src/VenuePlatform.BLL/Application/Features/Catering/CreateCateringOrderCommand.cs`
- `src/VenuePlatform.Web/Controllers/CateringController.cs`

**Dependencies:** Deliverable 4.1

**Acceptance criteria:**
- [ ] Catering options with lead times
- [ ] Orders linked to bookings
- [ ] Lead-time locking (cannot modify within 72h)
- [ ] Attendee dietary tracking

**Test scenario:**
1. Booking on 2026-03-15
2. Catering order locked at 2026-03-12 (72h before)
3. Try to modify on 2026-03-13 → Error "Order locked"

---

## Phase 5: Invoicing & Billing

### Deliverable 5.1: Invoice Generation

**Goal**: Create itemized invoices from bookings

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/Invoice.cs`
- `src/VenuePlatform.BLL/Domain/Entities/InvoiceItem.cs`
- `src/VenuePlatform.BLL/Application/Features/Invoices/GenerateInvoiceCommand.cs`
- `src/VenuePlatform.Web/Controllers/InvoicesController.cs`

**Dependencies:** Deliverable 4.2

**Acceptance criteria:**
- [ ] Invoice from booking (space + equipment + catering)
- [ ] Itemized line items
- [ ] Invoice number generation
- [ ] PDF export (optional)

**Test scenario:**
1. Create booking with 2 spaces, 3 equipment items, catering
2. Generate invoice
3. Invoice shows all line items with correct totals

---

### Deliverable 5.2: Split Billing

**Goal**: Split invoice across multiple payers

**Files to create:**
- `src/VenuePlatform.BLL/Domain/Entities/InvoiceSplit.cs`
- `src/VenuePlatform.BLL/Application/Features/Invoices/SplitInvoiceCommand.cs`
- `src/VenuePlatform.Web/Views/Invoices/Split.cshtml`

**Dependencies:** Deliverable 5.1

**Acceptance criteria:**
- [ ] Split by percentage or amount
- [ ] Multiple contacts as payers
- [ ] Track payment per split
- [ ] Feature-gated (Standard+)

**Test scenario:**
1. Invoice total: €1000
2. Split: Contact A pays €600 (catering + space), Contact B pays €400 (equipment)
3. System tracks both portions separately

---

## Phase 6: Testing & Polish

### Deliverable 6.1: Unit Tests - Core Domain

**Goal**: Unit tests for domain logic

**Files to create:**
- `tests/VenuePlatform.UnitTests/Domain/BookingTests.cs`
- `tests/VenuePlatform.UnitTests/Domain/CompanyTests.cs`
- `tests/VenuePlatform.UnitTests/Domain/EquipmentTests.cs`

**Dependencies:** All previous deliverables

**Acceptance criteria:**
- [ ] >80% code coverage for Domain layer
- [ ] Conflict detection tests
- [ ] Business rule validation tests

---

### Deliverable 6.2: Integration Tests

**Goal**: End-to-end API tests

**Files to create:**
- `tests/VenuePlatform.IntegrationTests/BookingApiTests.cs`
- `tests/VenuePlatform.IntegrationTests/TenantIsolationTests.cs`
- `tests/VenuePlatform.IntegrationTests/CustomWebApplicationFactory.cs`

**Dependencies:** Deliverable 6.1

**Acceptance criteria:**
- [ ] Full API flow tests
- [ ] Tenant isolation verified
- [ ] Soft delete behavior verified

---

## Immediate Next Steps (First Week)

1. **Day 1-2**: Deliverable 1.1 (Project Structure)
2. **Day 2-3**: Deliverable 1.2 (Base Domain)
3. **Day 3-4**: Deliverable 1.3 (Contracts)
4. **Day 4-5**: Deliverable 1.4 (DAL Infrastructure)
5. **Day 5**: Deliverable 2.1 (Identity Entities)

---

## Notes for Next AI

- All new projects use `net10.0` target framework
- Keep existing SQLite for development, PostgreSQL for production
- Use `Guid` for all primary keys
- Follow existing Identity pattern with custom entities
- Maintain strict tenant isolation on all queries
- Apply `[RequireFeature]` attribute for tier-gated features
- Use MediatR for all application layer operations
- Implement Result pattern for all commands/queries
