# 10 - Scope and Non-Functional Requirements

## Project Overview

**Conference and Event Venue Platform SaaS**

A multi-tenant SaaS platform for managing conference and event venue bookings, equipment allocation, catering services, and invoicing.

---

## Functional Scope

### Core Domains

| Domain | Description |
|--------|-------------|
| **Venue Management** | Configurable spaces that can be combined or subdivided |
| **Booking Management** | Reservations with conflict detection, recurring templates |
| **Equipment Management** | Inventory tracking with availability constraints |
| **Catering Management** | Menu options with lead-time requirements |
| **Client Management** | Contact records with history and notes |
| **Billing & Invoicing** | Itemized invoices with split billing support |
| **Tenant Management** | Multi-company support with tier-based features |

### Key Features

1. **Space Configuration**
   - Spaces can be combined or used independently
   - Capacity and layout configuration
   - Availability calendar

2. **Equipment Inventory**
   - Track equipment availability
   - Conflict detection for double-booking
   - Package definitions

3. **Catering with Lead Times**
   - Default 72-hour lead time for catering orders
   - Lead time locking (cannot modify within window)
   - Dietary requirement tracking

4. **Recurring Bookings**
   - Template-based recurring reservations
   - Weekly, monthly patterns
   - Bulk modification capability

5. **Client History**
   - Notes and interaction history
   - Past booking records
   - Preference tracking

6. **Invoicing**
   - Itemized billing (space, equipment, catering)
   - Split billing between multiple parties
   - Export capabilities

7. **Multi-Tenancy**
   - Path-based routing: `domain.com/{companySlug}`
   - Strict data isolation via CompanyId
   - Self-service tenant onboarding

---

## Non-Functional Requirements

### Performance

| Metric | Target |
|--------|--------|
| Page Load Time | < 2 seconds |
| API Response Time | < 500ms (p95) |
| Database Query Time | < 100ms |
| Concurrent Users | 1000 per tenant |
| Booking Search | < 200ms |

### Security

| Requirement | Implementation |
|-------------|----------------|
| Authentication | ASP.NET Core Identity with JWT |
| Authorization | Role-based + Permission-based |
| Tenant Isolation | CompanyId filtering on all queries |
| Data Protection | Encryption at rest and in transit |
| Audit Trail | All changes logged with user/timestamp |
| Soft Deletes | Business entities never hard-deleted |

### Scalability

- Support for 10,000+ tenants
- Horizontal scaling ready
- Database per tenant (future consideration)
- Caching strategy for common queries

### Availability

- 99.9% uptime target
- Database backups: daily with point-in-time recovery
- Zero-downtime deployments (future)

### Maintainability

- Code coverage: > 80%
- Architecture tests enforce layer boundaries
- Feature-based organization for parallel development
- Comprehensive XML documentation

---

## Constraints

1. **Technology Stack**
   - ASP.NET Core 10
   - Entity Framework Core
   - SQLite (initial), PostgreSQL (production)
   - Razor Pages + MVC

2. **Deployment**
   - Docker containerization (future)
   - Environment-based configuration

3. **Compliance**
   - GDPR-ready data handling
   - Audit trail retention: 7 years

---

## Out of Scope (Phase 1)

- Real-time notifications (WebSockets)
- Mobile application
- Payment gateway integration
- Advanced reporting/analytics dashboard
- Third-party calendar sync (Google/Outlook)
- API for external integrations
