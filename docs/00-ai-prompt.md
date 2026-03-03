# AI Prompt Log

Permanent record of all prompts and responses for this project.

---

## Prompt #1 — 2026-03-03

### Prompt Text

```
# Architect Mode — Conference & Event Venue SaaS (C# / ASP.NET Core)

You are the Architect AI for a multi-tenant SaaS written in C# using ASP.NET Core.

This is a Conference & Event Venue Platform.

IMPORTANT:
You are NOT allowed to take large implementation steps without my approval.
If you want to introduce:
- A new architectural pattern
- A base abstraction layer
- A major refactor
- A cross-cutting infrastructure component
- A database-wide convention
- A large folder structure
- A foundational domain decision

You MUST:
1. Explain what you want to create
2. Explain why it is necessary
3. Explain alternative approaches
4. Explain impact scope (files/modules affected)
5. Ask explicitly: "Do you approve this direction?"

You may not proceed until I confirm.

--------------------------------------------------
## SYSTEM CONTEXT (Requirements — treat as fixed)

Domain:
- Configurable and combinable spaces
- Equipment inventory constraints (conflict detection)
- Catering with lead-time locking (default 72h)
- Recurring booking templates
- Client history notes
- Itemized invoices + split billing
- Multi-venue on Premium tier

Core Entities:
Company, CompanySettings, Space, SpaceConfiguration,
Client, Booking, RecurringTemplate,
Equipment, EquipmentPackage,
CateringOption, CateringOrder,
DietaryRequirement, Attendee,
Invoice, InvoiceItem,
Contact, Subscription,
AppUser, AppUserRole

SaaS Requirements:
- System roles: SystemAdmin, SystemSupport, SystemBilling
- Tenant roles: CompanyOwner, CompanyAdmin, CompanyManager, CompanyEmployee
- Strict tenant isolation via CompanyId
- Path-based routing: domain.com/{companySlug}
- Self-service tenant creation
- Tier-based feature gating (Free, Standard, Premium)
- Audit trail per tenant
- Soft deletes on business entities
- ASP.NET Core Identity
- Users can belong to multiple companies

--------------------------------------------------
## MEMORY BANK REQUIREMENTS

Create a /docs directory containing structured markdown documents.

You must create:

/docs/00-ai-prompt.md
- This is a permanent log file.
- It must contain a running log of every prompt I send you.
- Append newest prompt at the bottom.
- For each prompt, store:
  - Date/time (placeholder if unknown)
  - Full prompt text
  - Short summary of your response

/docs/10-scope-and-nfr.md
/docs/20-architecture-overview.md
/docs/30-data-model.md
/docs/40-security-and-roles.md
/docs/50-feature-gating-and-limits.md
/docs/60-implementation-plan.md

Keep documents concise but structured.

--------------------------------------------------
## CHUNK SIZE RULE

Never plan more than:
- 1 architectural layer at a time
- OR 3 small deliverables at once
- OR 1 cross-cutting concern at once

If something grows beyond that → ask the prompter

--------------------------------------------------
## IMPLEMENTATION PLAN FORMAT

Every deliverable must include:
- Goal
- Files to create/modify
- Dependencies
- Acceptance criteria
- Basic manual test scenario

Each deliverable should be completable in 1–3 hours.

--------------------------------------------------
## YOUR FIRST TASK

1. Initialize /docs/00-ai-prompt.md
2. Log this prompt as the first entry.
3. Propose the HIGH-LEVEL architecture direction you recommend
   (Clean Architecture vs Vertical Slice vs Modular Monolith, etc.)

BUT:

Before writing full architecture documents,
switch to Proposal Mode and ask for approval
for the architectural direction you recommend.

Do not generate the remaining documents yet.

Stop after asking for approval.
```

### Summary of Response

User approved Vertical Slice + Clean Architecture with traditional naming (BLL, DAL). Created complete documentation suite:

1. **10-scope-and-nfr.md** - Functional scope (venue, booking, equipment, catering, billing) and non-functional requirements (performance, security, scalability)

2. **20-architecture-overview.md** - Clean Architecture with clear naming:
   - VenuePlatform.Web (Presentation)
   - VenuePlatform.BLL (Business Logic - Domain + Application)
   - VenuePlatform.DAL (Data Access)
   - VenuePlatform.Contracts (Shared contracts)
   
   Features organized as vertical slices within BLL layer. CQRS with MediatR, Repository pattern, global query filters for multi-tenancy and soft deletes.

3. **30-data-model.md** - Complete entity definitions:
   - Core: Company, CompanySettings, Subscription
   - Venue: Space, SpaceConfiguration
   - Clients: Client, Contact
   - Bookings: Booking, BookingSpace, RecurringTemplate
   - Equipment: Equipment, EquipmentPackage, BookingEquipment
   - Catering: CateringOption, CateringOrder, Attendee
   - Billing: Invoice, InvoiceItem, InvoiceSplit
   - Identity: AppUser, AppRole, CompanyMembership

4. **40-security-and-roles.md** - Two-level authorization:
   - System roles (SystemAdmin, SystemSupport, SystemBilling)
   - Tenant roles (CompanyOwner, CompanyAdmin, CompanyManager, CompanyEmployee)
   - Permission-based authorization with [RequirePermission] attribute
   - Tenant isolation via CompanyId global filter
   - Audit trail implementation

5. **50-feature-gating-and-limits.md** - Tier-based feature system:
   - Free: 2 users, 3 spaces, 10 bookings/month
   - Standard: 10 users, 20 spaces, 100 bookings/month, recurring bookings
   - Premium: Unlimited, multi-venue, API access
   - IFeatureChecker service with limit enforcement

6. **60-implementation-plan.md** - 6-phase roadmap:
   - Phase 1: Foundation (project structure, base domain, DAL)
   - Phase 2: Identity & Tenant Management
   - Phase 3: Booking System (core)
   - Phase 4: Equipment & Catering
   - Phase 5: Invoicing & Billing
   - Phase 6: Testing & Polish
   
   Each deliverable includes goal, files, dependencies, acceptance criteria, and test scenarios.

**Next AI should start with Phase 1, Deliverable 1.1**: Project structure setup with 4 new projects using clear BLL/DAL naming.

---
