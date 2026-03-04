## 2026-03-03T22:38:04Z

**Summary:** Patch: Removed reflection-based booking field updates; added type-safe Booking.UpdateDetails method.

### Prompt Text

```
# Coder Mode — Patch: Remove reflection-based updates for Booking fields (KISS)

## Goal
Remove reflection-based assignment for `Booking` fields (Title, StartUtc, EndUtc, AttendeeCount).
Replace with a type-safe domain method.

No behavior changes. No schema changes. No endpoint shape changes.

---

## Hard constraints
- net10.0
- Minimal changes
- Do NOT change migrations / schema
- Do NOT change endpoint routes or request/response DTOs
- Do NOT introduce new services/repositories
- No reflection for domain writes in Web

---

## Logging
Append this prompt to `docs/ai-prompts.md` with date/time placeholder + summary:
"Patch: Removed reflection-based booking field updates; added type-safe Booking.UpdateDetails method."

---

## Step 1 — Add a type-safe update method to Booking

Edit:
`VenuePlatform.BLL/Domain/Bookings/Booking.cs`

Add:

```csharp
public void UpdateDetails(string title, DateTime startUtc, DateTime endUtc, int attendeeCount)
{
    if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
    title = title.Trim();
    if (title.Length > 200) throw new ArgumentException("Title must be at most 200 characters.", nameof(title));
    if (startUtc >= endUtc) throw new ArgumentException("StartUtc must be before EndUtc.");
    if (attendeeCount < 0) throw new ArgumentOutOfRangeException(nameof(attendeeCount), "AttendeeCount must be >= 0.");

    Title = title;
    StartUtc = startUtc;
    EndUtc = endUtc;
    AttendeeCount = attendeeCount;
}
```

Notes:
- Keep validation consistent with constructor rules.
- This method replaces reflection usage.
- No conflict/min-duration checks here (those stay in endpoints).

---

## Step 2 — Replace reflection in Program.cs with UpdateDetails()

Edit:
`VenuePlatform.Web/Program.cs`

Find the reflection-based sets of:
- Title
- StartUtc
- EndUtc
- AttendeeCount

Replace them with:

```csharp
booking.UpdateDetails(request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);
```

Apply to:
- existing `PUT /{companySlug}/bookings/{id}` (if it updates fields)
- `PUT /{companySlug}/bookings/{id}/with-spaces`
- any other place reflection is used for those properties

Do NOT change validation behavior beyond removing reflection; endpoint validation remains as-is.

---

## Step 3 — Verification

- `dotnet build` => 0 warnings, 0 errors
- Manual smoke:
  - Update booking title/time/attendees -> works
  - Min duration + conflict detection + pricing behavior unchanged
  - Cancelled booking update still rejected as before

STOP AFTER IMPLEMENTING.
Do NOT implement new features in this patch.
```

### Summary of Response

Patch: Removed reflection-based booking field updates; added type-safe Booking.UpdateDetails method.

**Implementation Complete:**

**Changes to `VenuePlatform.BLL/Domain/Bookings/Booking.cs`:**
- Added `UpdateDetails(string title, DateTime startUtc, DateTime endUtc, int attendeeCount)` method
- Validation includes:
  - Title required and trimmed
  - Title max 200 characters
  - StartUtc must be before EndUtc
  - AttendeeCount must be >= 0

**Changes to `VenuePlatform.Web/Program.cs`:**
- Replaced reflection-based updates in `PUT /{companySlug}/bookings/{id}` endpoint (lines 1722-1726)
- Replaced reflection-based updates in `PUT /{companySlug}/bookings/{id}/with-spaces` endpoint (lines 2108-2112)
- Both now use: `booking.UpdateDetails(request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);`

**No Changes To:**
- Migrations/schema
- Endpoint routes or DTOs
- Validation behavior (endpoint validation remains as-is)
- Conflict detection, min duration checks, or pricing logic

**Build Status:** Verified successful (0 warnings, 0 errors)

**Target Framework:** net10.0 confirmed

---

## 2026-03-03T22:52:00Z

**Summary:** Phase 6.2: Added BookingStatus (Pending/Confirmed) and confirm endpoint.

### Prompt Text

```
# Coder Mode — Phase 6.2: Booking status (Pending/Confirmed) + confirm endpoint (KISS)

## Goal
Introduce a minimal booking status field to support basic lifecycle:
- Pending (default)
- Confirmed
- Cancelled (still represented by IsCancelled OR status; pick ONE approach and keep it consistent)

Implement:
- a confirm endpoint
- keep existing cancel behavior intact

No approvals, no notifications, no workflows.

---

## Hard constraints
- net10.0
- Minimal APIs only
- No new services/repositories
- No CQRS/MediatR/AutoMapper
- Keep changes minimal and explicit
- Must preserve existing behavior and endpoints

---

## Logging
Append this prompt to `docs/ai-prompts.md` with date/time placeholder + summary:
"Phase 6.2: Added BookingStatus (Pending/Confirmed) and confirm endpoint."

---

## Step 1 — Decide representation (KISS)

Current model uses:
- `IsCancelled` boolean

We will add:
- `BookingStatus` enum with values:
  - Pending = 0
  - Confirmed = 1

Cancellation stays as `IsCancelled` (DO NOT replace it with enum cancellation in this phase).
Reason: minimal schema change and preserves existing semantics.

---

## Step 2 — Contracts: BookingStatus enum

Create:
`VenuePlatform.Contracts/Bookings/BookingStatus.cs`

```csharp
namespace VenuePlatform.Contracts.Bookings;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1
}
```

---

## Step 3 — Domain: add Status to Booking

Edit:
`VenuePlatform.BLL/Domain/Bookings/Booking.cs`

Add:
- `public BookingStatus Status { get; private set; }`

Rules:
- Default Status = Pending on creation
- Cancelled bookings cannot be confirmed

Add methods:
- `public void Confirm()`
  - if IsCancelled -> throw InvalidOperationException("Cancelled booking cannot be confirmed.")
  - set Status = Confirmed
- (Optional) `public void SetPending()` is NOT needed.

Use BookingStatus from Contracts (acceptable since BLL already references Contracts).

---

## Step 4 — DbContext + Migration

Edit Booking config in `ApplicationDbContext`:
- Map `Status` as int (default)

Migration:
- `dotnet ef migrations add AddBookingStatus -p VenuePlatform.DAL -s VenuePlatform.Web`
- `dotnet ef database update -p VenuePlatform.DAL -s VenuePlatform.Web`

---

## Step 5 — Contracts: include Status in responses

Update:
`VenuePlatform.Contracts/Bookings/BookingResponse.cs`
Add:
- `BookingStatus Status`

Update:
`VenuePlatform.Contracts/Bookings/BookingDetailsResponse.cs`
Add:
- `BookingStatus Status`

Ensure all mapping code in endpoints populates it.

---

## Step 6 — Endpoint: confirm booking

Add under tenant route group:

POST `/{companySlug}/bookings/{id:guid}/confirm`

Access:
- Auth required
- Role: CompanyManager / CompanyAdmin / CompanyOwner

Behavior:
- Load booking (tenant filtered)
- 404 if not found
- 400 if IsCancelled
- Set Status = Confirmed using domain method `Confirm()`
- SaveChanges
- Return 200 with BookingResponse OR 204 NoContent (pick one; prefer 200 with BookingResponse for UI convenience)

Idempotency:
- If already confirmed -> return 204 or 200 with same state (pick consistent behavior; prefer 204 NoContent)

---

## Step 7 — Ensure existing endpoints preserve status rules (minimal)

- POST booking endpoints should set Status = Pending (implicitly via constructor or default)
- PUT update endpoints should not change Status (unless you explicitly decide otherwise; default is: unchanged)
- DELETE cancel endpoint should keep Status as-is and just set IsCancelled = true

---

## Acceptance criteria

- New bookings are Pending
- Confirm endpoint sets booking to Confirmed
- Confirming a cancelled booking returns 400
- Confirming an already confirmed booking is idempotent
- Responses include Status
- Tenant isolation intact
- Build passes (0 warnings, 0 errors)

STOP AFTER IMPLEMENTING.
Do NOT implement:
- approval workflows
- audit trails
- notifications
- invoice generation
```

### Summary of Response

Phase 6.2: Added BookingStatus (Pending/Confirmed) and confirm endpoint.

**Implementation Complete:**

**Changes to `VenuePlatform.Contracts/Bookings/BookingStatus.cs`:**
- Created new enum with values:
  - `Pending = 0`
  - `Confirmed = 1`

**Changes to `VenuePlatform.BLL/Domain/Bookings/Booking.cs`:**
- Added `using VenuePlatform.Contracts.Bookings;` import
- Added `public BookingStatus Status { get; private set; }` property
- Constructor sets `Status = BookingStatus.Pending` by default
- Added `Confirm()` method:
  - Throws `InvalidOperationException` if booking is cancelled
  - Sets `Status = BookingStatus.Confirmed`

**Changes to `VenuePlatform.DAL/Persistence/ApplicationDbContext.cs`:**
- Added `booking.Property(b => b.Status).IsRequired();` configuration

**Migration Files Created:**
- `20260303224503_AddBookingStatus.cs` - Adds Status column to Bookings table
- `20260303224503_AddBookingStatus.Designer.cs` - Migration designer file
- Updated `ApplicationDbContextModelSnapshot.cs` - Added Status property to Booking entity

**Changes to `VenuePlatform.Contracts/Bookings/BookingResponse.cs`:**
- Added `BookingStatus Status` parameter to record

**Changes to `VenuePlatform.Contracts/Bookings/BookingDetailsResponse.cs`:**
- Added `BookingStatus Status` parameter to record

**Changes to `VenuePlatform.Web/Program.cs`:**
- Updated all `BookingResponse` instantiations to include `Status` property:
  - GET /bookings list endpoint
  - POST /bookings create endpoint
  - POST /bookings/with-spaces create endpoint
  - GET /bookings/{id} endpoint
  - PUT /bookings/{id} update endpoint
  - PUT /bookings/{id}/spaces endpoint
  - PUT /bookings/{id}/with-spaces endpoint
- Updated GET /bookings/{id}/details endpoint:
  - Updated anonymous type projection to include `Status`
  - Updated `BookingDetailsResponse` instantiation to include `Status`
- Added new confirm endpoint `POST /{companySlug}/bookings/{id:guid}/confirm`:
  - Requires authentication
  - Requires CompanyManager, CompanyAdmin, or CompanyOwner role
  - Returns 404 if booking not found
  - Returns 400 if booking is cancelled
  - Returns 204 NoContent if already confirmed (idempotent)
  - Sets status to Confirmed and returns 204 NoContent on success

**Build Status:** Verified successful (0 warnings, 0 errors)

**Target Framework:** net10.0 confirmed

---

## 2026-03-03T22:55:00Z

**Summary:** Phase 6.3: Blocked modifications to confirmed bookings (updates require Pending).

### Prompt Text

```
# Coder Mode — Phase 6.3: Disallow edits to confirmed bookings (KISS)

## Goal
Prevent modification of confirmed bookings to keep scheduling stable.
Once a booking is confirmed, it cannot be edited via update endpoints.

This phase does NOT add an "unconfirm" endpoint.

---

## Hard constraints
- net10.0
- Minimal APIs only
- No schema changes
- No new services/repositories
- No CQRS/MediatR/AutoMapper
- Keep change surface minimal

---

## Logging
Append this prompt to `docs/ai-prompts.md` with date/time placeholder + summary:
"Phase 6.3: Blocked modifications to confirmed bookings (updates require Pending)."

---

## Rule (KISS)

For any endpoint that modifies booking fields or spaces:

- If booking.IsCancelled == true -> 400 (already exists)
- Else if booking.Status == Confirmed -> 400 with message:
  "Confirmed booking cannot be modified."

Endpoints affected:
- PUT /{companySlug}/bookings/{id}
- PUT /{companySlug}/bookings/{id}/spaces
- PUT /{companySlug}/bookings/{id}/with-spaces

Endpoints NOT affected:
- POST create endpoints (always Pending)
- POST confirm endpoint
- DELETE cancel endpoint (cancellation is allowed even if confirmed)

---

## Step 1 — Apply checks (minimal)

In each affected endpoint, after loading booking:
- Add the confirmed check
- Return Results.BadRequest(new { error = "Confirmed booking cannot be modified." })

Keep response shape consistent with existing bad requests.

---

## Step 2 — Acceptance criteria

- Confirmed booking:
  - update time -> 400
  - replace spaces -> 400
  - update with spaces -> 400
- Pending booking: unchanged behavior
- Cancelled booking: unchanged behavior (still 400 on update)
- Confirm endpoint still works on Pending
- Cancel endpoint still works on Confirmed
- Build passes (0 warnings, 0 errors)

STOP AFTER IMPLEMENTING.
Do NOT implement:
- unconfirm endpoint
- audit logs
- approval workflows
```

### Summary of Response

Phase 6.3: Blocked modifications to confirmed bookings (updates require Pending).

**Implementation Complete:**

**Changes to `VenuePlatform.Web/Program.cs`:**
Added confirmed booking check to three PUT endpoints:

1. `PUT /{companySlug}/bookings/{id}` (lines 1712-1716):
   - Added check: `if (booking.Status == BookingStatus.Confirmed)`
   - Returns 400 with message: "Confirmed booking cannot be modified."

2. `PUT /{companySlug}/bookings/{id}/spaces` (lines 1892-1896):
   - Added check: `if (booking.Status == BookingStatus.Confirmed)`
   - Returns 400 with message: "Confirmed booking cannot be modified."

3. `PUT /{companySlug}/bookings/{id}/with-spaces` (lines 2062-2066):
   - Added check: `if (booking.Status == BookingStatus.Confirmed)`
   - Returns 400 with message: "Confirmed booking cannot be modified."

All checks are placed immediately after the existing cancelled booking checks.

**Behavior:**
- Confirmed bookings cannot be modified via any update endpoint
- Pending bookings: unchanged behavior (updates allowed)
- Cancelled bookings: unchanged behavior (still blocked with 400)
- Confirm endpoint: still works (can confirm Pending bookings)
- Cancel endpoint: still works (can cancel Confirmed bookings)

**Build Status:** Verified successful (0 warnings, 0 errors)

**Target Framework:** net10.0 confirmed

**No Changes To:**
- Database schema
- Response DTOs
- Confirm or Cancel endpoints
- Create endpoints

---

## 2026-03-03T22:58:50Z

**Summary:** Phase 6.4: Added booking cancellation metadata (CancelledUtc, CancelReason).

### Prompt Text

```
# Coder Mode — Phase 6.4: Cancellation metadata (CancelledUtc + CancelReason), KISS

## Goal
Enhance booking cancellation by storing:
- `CancelledUtc` (timestamp)
- `CancelReason` (optional short text)

Keep cancellation idempotent.
Do NOT change conflict detection logic beyond the fact that cancelled bookings already do not block.

---

## Hard constraints
- net10.0
- Minimal APIs only
- No CQRS/MediatR/AutoMapper
- No new services/repositories
- Keep changes minimal and explicit
- Existing cancel endpoint remains the way to cancel

---

## Logging
Append this prompt to `docs/ai-prompts.md` with date/time placeholder + summary:
"Phase 6.4: Added booking cancellation metadata (CancelledUtc, CancelReason)."

---

## Step 1 — Domain: extend Booking

Edit:
`VenuePlatform.BLL/Domain/Bookings/Booking.cs`

Add:
- `public DateTime? CancelledUtc { get; private set; }`
- `public string? CancelReason { get; private set; }`

Add method:
```csharp
public void Cancel(string? reason, DateTime cancelledUtc)
{
    if (IsCancelled) return; // idempotent

    IsCancelled = true;
    CancelledUtc = cancelledUtc;

    if (!string.IsNullOrWhiteSpace(reason))
    {
        reason = reason.Trim();
        if (reason.Length > 500) reason = reason[..500];
        CancelReason = reason;
    }
}
```

Notes:
- Keep it simple: truncate instead of rejecting if too long.
- Do NOT change Status here (Confirmed/Pending remains as-is).

---

## Step 2 — DAL: mapping + migration

Edit Booking configuration in `ApplicationDbContext`:
- Map `CancelledUtc` nullable
- Map `CancelReason` nullable, max length 500

Migration:
- `dotnet ef migrations add AddBookingCancellationMetadata -p VenuePlatform.DAL -s VenuePlatform.Web`
- `dotnet ef database update -p VenuePlatform.DAL -s VenuePlatform.Web`

---

## Step 3 — Contracts: expose cancellation metadata

Update:
`VenuePlatform.Contracts/Bookings/BookingResponse.cs`
Add:
- `DateTime? CancelledUtc`
- `string? CancelReason`

Update:
`VenuePlatform.Contracts/Bookings/BookingDetailsResponse.cs`
Add:
- `DateTime? CancelledUtc`
- `string? CancelReason`

Ensure all mapping code populates these fields.

---

## Step 4 — Endpoint: accept cancel reason

Update existing cancel endpoint:

DELETE `/{companySlug}/bookings/{id}`

Add optional query parameter:
- `reason` (string?, optional)

Behavior:
- Load booking (tenant filtered)
- If not found -> 404
- Call `booking.Cancel(reason, DateTime.UtcNow)`
- SaveChanges
- Return 204 NoContent

Idempotency:
- If already cancelled -> still 204 and do not overwrite existing CancelReason/CancelledUtc.

---

## Acceptance criteria

- Cancel sets IsCancelled=true and sets CancelledUtc
- If reason provided, CancelReason stored (trimmed, max 500)
- Cancel is idempotent (second call keeps original metadata)
- Cancelled bookings still do not block availability/conflicts
- Responses include CancelledUtc/CancelReason
- Build passes (0 warnings, 0 errors)

STOP AFTER IMPLEMENTING.
Do NOT implement:
- audit trail
- cancel permissions changes
- un-cancel
- refunds/invoicing rules
```

### Summary of Response

Phase 6.4: Added booking cancellation metadata (CancelledUtc, CancelReason).

**Implementation Complete:**

**Changes to `VenuePlatform.BLL/Domain/Bookings/Booking.cs`:**
- Added `public DateTime? CancelledUtc { get; private set; }` property
- Added `public string? CancelReason { get; private set; }` property
- Updated `Cancel()` method to `Cancel(string? reason, DateTime cancelledUtc)`:
  - Idempotent: returns early if already cancelled
  - Sets IsCancelled, CancelledUtc, and CancelReason
  - Trims reason and limits to 500 characters

**Changes to `VenuePlatform.DAL/Persistence/ApplicationDbContext.cs`:**
- Added `booking.Property(b => b.CancelledUtc).IsRequired(false);`
- Added `booking.Property(b => b.CancelReason).IsRequired(false).HasMaxLength(500);`

**Changes to `VenuePlatform.Contracts/Bookings/BookingResponse.cs`:**
- Added `DateTime? CancelledUtc` parameter to record
- Added `string? CancelReason` parameter to record

**Changes to `VenuePlatform.Contracts/Bookings/BookingDetailsResponse.cs`:**
- Added `DateTime? CancelledUtc` parameter to record
- Added `string? CancelReason` parameter to record

**Changes to `VenuePlatform.Web/Program.cs`:**
- Updated DELETE cancel endpoint to accept optional `reason` query parameter
- Changed `booking.Cancel()` call to `booking.Cancel(reason, DateTime.UtcNow)`
- Updated all `BookingResponse` instantiations to include `CancelledUtc` and `CancelReason`
- Updated all `BookingDetailsResponse` instantiations to include `CancelledUtc` and `CancelReason`
- Updated all booking query projections to include `CancelledUtc` and `CancelReason`

**Build Status:** Verified successful (0 warnings, 0 errors)

**Target Framework:** net10.0 confirmed

---

## 2026-03-03T23:27:00Z

**Summary:** Phase 6.5: Added booking actor fields for create/confirm/cancel actions.

### Prompt Text

```
# Coder Mode — Phase 6.5: Booking actor fields (CreatedByUserId, ConfirmedByUserId, CancelledByUserId), KISS

## Goal
Store which user performed key booking actions:
- CreatedByUserId
- ConfirmedByUserId
- CancelledByUserId

This is Booking-only audit metadata (NOT a platform-wide audit trail system).

No new middleware, no generic audit framework.

---

## Hard constraints
- net10.0
- Minimal APIs only
- No new services/repositories
- No CQRS/MediatR/AutoMapper
- Keep it explicit and minimal
- Use current authenticated user id from JWT claims (sub)

---

## Logging
Append this prompt to `docs/ai-prompts.md` with date/time placeholder + summary:
"Phase 6.5: Added booking actor fields for create/confirm/cancel actions."

---

## Step 1 — Domain: add actor fields + setters

Edit:
`VenuePlatform.BLL/Domain/Bookings/Booking.cs`

Add:
- `public Guid? CreatedByUserId { get; private set; }`
- `public Guid? ConfirmedByUserId { get; private set; }`
- `public Guid? CancelledByUserId { get; private set; }`

Rules:
- CreatedByUserId set once at create
- ConfirmedByUserId set when confirming (only if transition happens)
- CancelledByUserId set when cancelling (only if transition happens)
- Idempotent endpoints must not overwrite these once set

Add small methods (KISS, type-safe):
- `public void SetCreatedBy(Guid userId)` (no-op if already set)
- Update `Confirm()` to accept userId or add `Confirm(Guid userId)` overload:
  - if already confirmed -> no-op
  - if cancelled -> throw as before
  - set Status=Confirmed and ConfirmedByUserId=userId (only if it wasn't confirmed)
- Update `Cancel(...)` to accept userId or add `Cancel(string? reason, DateTime cancelledUtc, Guid userId)` overload:
  - if already cancelled -> return (do not overwrite CancelledByUserId)

Choose the cleanest signature set; avoid duplicating logic.

---

## Step 2 — DAL: mapping + migration

Edit Booking configuration in `ApplicationDbContext`:
- Map the three actor fields as nullable Guid columns

Migration:
- `dotnet ef migrations add AddBookingActorFields -p VenuePlatform.DAL -s VenuePlatform.Web`
- `dotnet ef database update -p VenuePlatform.DAL -s VenuePlatform.Web`

---

## Step 3 — Contracts: expose actor fields (optional but useful)

Update:
`VenuePlatform.Contracts/Bookings/BookingResponse.cs`
Add:
- `Guid? CreatedByUserId`
- `Guid? ConfirmedByUserId`
- `Guid? CancelledByUserId`

Update:
`VenuePlatform.Contracts/Bookings/BookingDetailsResponse.cs`
Add the same.

Update mapping code accordingly.

---

## Step 4 — Web: extract current user id (KISS)

In endpoints where needed, extract user id from claims:
- `sub` claim contains Guid
- If missing/invalid -> return 401 (should not happen if auth is set correctly)

Do NOT introduce a new ICurrentUser service in this phase; keep it local and explicit.

---

## Step 5 — Apply in endpoints

### A) Create endpoints
- POST /bookings
- POST /bookings/with-spaces

After creating Booking instance:
- set CreatedByUserId from current user

### B) Confirm endpoint
POST /bookings/{id}/confirm
When confirming:
- set ConfirmedByUserId as part of Confirm transition

### C) Cancel endpoint
DELETE /bookings/{id}?reason=
When cancelling:
- set CancelledByUserId as part of Cancel transition
- keep idempotent behavior (don't overwrite existing actor if already cancelled)

---

## Acceptance criteria

- New booking stores CreatedByUserId
- Confirm sets ConfirmedByUserId only once
- Cancel sets CancelledByUserId only once, preserves on repeated cancels
- Actor fields appear in responses (if you implemented Step 3)
- No schema or behavior changes beyond metadata
- Build passes (0 warnings, 0 errors)

STOP AFTER IMPLEMENTING.
Do NOT implement:
- generic audit trail system
- system roles audit features
- per-entity audit base classes
```

### Summary of Response

Phase 6.5: Added booking actor fields for create/confirm/cancel actions.

**Implementation Complete:**

**Changes to `VenuePlatform.BLL/Domain/Bookings/Booking.cs`:**
- Added `public Guid? CreatedByUserId { get; private set; }` property
- Added `public Guid? ConfirmedByUserId { get; private set; }` property
- Added `public Guid? CancelledByUserId { get; private set; }` property
- Updated `Cancel(string? reason, DateTime cancelledUtc, Guid userId)` method:
  - Sets `CancelledByUserId = userId` only when transitioning to cancelled state
  - Idempotent: does not overwrite if already cancelled
- Updated `Confirm(Guid userId)` method:
  - Sets `ConfirmedByUserId = userId` only when transitioning from Pending to Confirmed
  - Idempotent: no-op if already confirmed
- Added `SetCreatedBy(Guid userId)` method:
  - Sets `CreatedByUserId = userId` only if not already set
  - Idempotent: no-op if already set

**Changes to `VenuePlatform.DAL/Persistence/ApplicationDbContext.cs`:**
- Added `booking.Property(b => b.CreatedByUserId).IsRequired(false);`
- Added `booking.Property(b => b.ConfirmedByUserId).IsRequired(false);`
- Added `booking.Property(b => b.CancelledByUserId).IsRequired(false);`

**Migration Files Created:**
- `20260303232744_AddBookingActorFields.cs` - Adds three actor columns to Bookings table
- `20260303232744_AddBookingActorFields.Designer.cs` - Migration designer file
- Updated `ApplicationDbContextModelSnapshot.cs` - Added actor properties to Booking entity

**Changes to `VenuePlatform.Contracts/Bookings/BookingResponse.cs`:**
- Added `Guid? CreatedByUserId` parameter to record
- Added `Guid? ConfirmedByUserId` parameter to record
- Added `Guid? CancelledByUserId` parameter to record

**Changes to `VenuePlatform.Contracts/Bookings/BookingDetailsResponse.cs`:**
- Added `Guid? CreatedByUserId` parameter to record
- Added `Guid? ConfirmedByUserId` parameter to record
- Added `Guid? CancelledByUserId` parameter to record

**Changes to `VenuePlatform.Web/Program.cs`:**
- Updated POST /bookings endpoint:
  - Calls `booking.SetCreatedBy(userId)` after creating booking
- Updated POST /bookings/with-spaces endpoint:
  - Calls `booking.SetCreatedBy(userId)` after creating booking
- Updated POST /bookings/{id}/confirm endpoint:
  - Changed `booking.Confirm()` to `booking.Confirm(userId)`
- Updated DELETE /bookings/{id} endpoint:
  - Changed `booking.Cancel(reason, DateTime.UtcNow)` to `booking.Cancel(reason, DateTime.UtcNow, userId)`
- Updated all `BookingResponse` instantiations to include actor fields
- Updated all `BookingDetailsResponse` instantiations to include actor fields
- Updated all booking query projections to include actor fields

**Build Status:** Verified successful (0 warnings, 0 errors)

**Target Framework:** net10.0 confirmed

---

## 2026-03-03T23:32:33Z

**Summary:** Safe refactor: split Program.cs endpoints into endpoint modules; no behavior change.

### Prompt Text

```
# Coder Mode — SAFE REFACTOR: Split `Program.cs` into endpoint modules (NO behavior changes)

## Goal
`VenuePlatform.Web/Program.cs` is ~2200 lines. Refactor by extracting endpoints into feature files while keeping:
- **all routes identical**
- **all auth/role checks identical**
- **all validation logic identical**
- **all helper functions identical**
- **middleware order identical**
- **DI registrations identical**
- **Swagger behavior identical**
- **responses identical**

This is a **pure refactor**: compile-safe, minimal, and reversible.

---

## Hard constraints (must obey)
- net10.0
- Minimal APIs only (no Controllers/MVC)
- No MediatR/CQRS/AutoMapper
- No new services/repositories
- Do NOT change DAL/BLL/Contracts
- Do NOT change database schema/migrations
- Do NOT change endpoint paths, verbs, query params, payload shapes, or status codes
- Do NOT change authorization logic or membership checks
- Do NOT change tenant resolution behavior or exempt routes
- Helpers may be moved, but logic must remain byte-for-byte equivalent (only signature adjustments if needed)

---

## Logging
Append this prompt to `docs/ai-prompts.md` with date/time placeholder + summary:
"Safe refactor: split Program.cs endpoints into endpoint modules; no behavior change."

---

## Output requirement
At the end, provide:
- list of new files created
- what `Program.cs` contains after refactor (high-level)
- confirmation that all routes remain identical
- confirmation: `dotnet build` succeeds (0 warnings, 0 errors)

---

# Step 1 — Create Web folder structure

Create folder in `VenuePlatform.Web`:

`Endpoints/`

Create these files (static classes + extension methods):

1) `VenuePlatform.Web/Endpoints/AuthEndpoints.cs`
2) `VenuePlatform.Web/Endpoints/ClientEndpoints.cs`
3) `VenuePlatform.Web/Endpoints/SpaceEndpoints.cs`
4) `VenuePlatform.Web/Endpoints/SpaceConfigurationEndpoints.cs`
5) `VenuePlatform.Web/Endpoints/BookingEndpoints.cs`
6) `VenuePlatform.Web/Endpoints/DevEndpoints.cs` (dev-only routes)
7) `VenuePlatform.Web/Endpoints/HealthEndpoints.cs` (platform + tenant health endpoints, if present)

Keep naming consistent and KISS.

---

# Step 2 — Keep `Program.cs` as the composition root only

After refactor, `Program.cs` should primarily contain:

- builder creation + configuration
- service registrations (DI) (unchanged)
- middleware pipeline setup (unchanged order)
- swagger setup (unchanged)
- creation of tenant route group(s) (unchanged)
- calls to `Map...Endpoints()` extension methods

Do NOT move DI registrations or middleware out of Program.cs.

---

# Step 3 — Extension method pattern (KISS)

Each endpoint file should contain one public extension method, e.g.:

```csharp
namespace VenuePlatform.Web.Endpoints;

public static class BookingEndpoints
{
    public static RouteGroupBuilder MapBookingEndpoints(this RouteGroupBuilder group)
    {
        // all existing booking routes mapped here
        return group;
    }
}
```

Notes:
- Use `RouteGroupBuilder` for tenant-scoped endpoints.
- Use `IEndpointRouteBuilder` for non-tenant endpoints (auth, platform health, swagger helper endpoints if any).
- Do NOT change route templates.

---

# Step 4 — Move endpoints by feature WITHOUT changing code behavior

Move the endpoint mappings and handler lambdas from `Program.cs` into the appropriate modules:

- Auth endpoints -> `AuthEndpoints.cs`
- Clients -> `ClientEndpoints.cs`
- Spaces + availability -> `SpaceEndpoints.cs`
- Space configurations (and join endpoints) -> `SpaceConfigurationEndpoints.cs`
- Bookings:
  - list, details, create, with-spaces, update, with-spaces update, confirm, cancel, set spaces, conflict helpers, min-duration helpers, pricing helpers, current-user-id extraction helper -> `BookingEndpoints.cs`
- Dev-only endpoints -> `DevEndpoints.cs`
- Health endpoints (platform + tenant) -> `HealthEndpoints.cs`

**Move code, don't rewrite it.**

---

# Step 5 — Handle shared helpers safely (no behavior change)

You likely have shared local helper functions in Program.cs:
- `FindConflictingBookings`
- `ValidateMinBookingDurationAsync`
- `CalculateBookingTotal`
- current user id extraction helper
- any "missing ids" helper

Rule:
- If a helper is only used by one feature, move it into that feature file as a `private static` method.
- If a helper is used by multiple feature files, create ONE shared internal helper file:

Create:
`VenuePlatform.Web/Endpoints/EndpointHelpers.cs`

---

# Step 6 — Preserve tenant route grouping exactly

If currently you do:
- `var tenantGroup = app.MapGroup("/{companySlug}");`

Keep it and pass that group into module mapping:

```csharp
tenantGroup.MapBookingEndpoints();
```

---

# Step 7 — Build + sanity checks

After moving code:
1) Run:
   - `dotnet build`
2) Quick manual smoke (no need to add tests):
   - `GET /health`
   - `POST /auth/login` (if present)
   - `GET /{companySlug}/spaces/availability?...`
   - `GET /{companySlug}/bookings?...`
   - `POST /{companySlug}/bookings/with-spaces` (dev user)
   - `POST /{companySlug}/bookings/{id}/confirm`
   - `DELETE /{companySlug}/bookings/{id}?reason=test`

Confirm behavior unchanged.

---

## Acceptance criteria
- `Program.cs` reduced substantially (ideally < 400 lines)
- Endpoints compile and run
- No route changes, no behavior changes
- No new dependencies or architectural layers
- Build passes: 0 warnings, 0 errors

STOP AFTER IMPLEMENTING THIS REFACTOR.
Do NOT implement new features while refactoring.
```

### Summary of Response

Safe refactor: split Program.cs endpoints into endpoint modules; no behavior change.

**New Files Created:**

1. **`VenuePlatform.Web/Endpoints/EndpointHelpers.cs`** - Shared internal helper methods:
   - `GetUserIdFromClaims(ClaimsPrincipal user)` - Extracts user ID from JWT "sub" claim
   - `FindConflictingBookings(...)` - Detects booking space conflicts
   - `ValidateMinBookingDurationAsync(...)` - Validates minimum booking duration against SpaceConfiguration
   - `CalculateBookingTotal(...)` - Calculates booking total amount based on duration and rates

2. **`VenuePlatform.Web/Endpoints/AuthEndpoints.cs`** - Authentication endpoints (non-tenant):
   - `POST /auth/login` - User login with JWT token generation

3. **`VenuePlatform.Web/Endpoints/HealthEndpoints.cs`** - Health check endpoints:
   - `GET /health` - Platform health (non-tenant)
   - `GET /{companySlug}/health` - Tenant health
   - `GET /{companySlug}/db-check` - DbContext/Repository wiring check

4. **`VenuePlatform.Web/Endpoints/ClientEndpoints.cs`** - Client management (tenant-scoped):
   - `POST /{companySlug}/clients/seed-one` - Create test client (Manager+)
   - `GET /{companySlug}/clients` - List clients (any member)
   - `GET /{companySlug}/memberships` - List memberships

5. **`VenuePlatform.Web/Endpoints/SpaceEndpoints.cs`** - Space management (tenant-scoped):
   - `GET /{companySlug}/spaces` - List spaces
   - `POST /{companySlug}/spaces` - Create space (Manager+)
   - `GET /{companySlug}/spaces/{id}` - Get space by ID
   - `POST /{companySlug}/spaces/{id}/deactivate` - Deactivate space (Manager+)
   - `GET /{companySlug}/spaces/availability` - Search available spaces

6. **`VenuePlatform.Web/Endpoints/SpaceConfigurationEndpoints.cs`** - Space configurations (tenant-scoped):
   - `GET /{companySlug}/space-configurations` - List configurations
   - `POST /{companySlug}/space-configurations` - Create configuration (Manager+)
   - `GET /{companySlug}/space-configurations/{id}` - Get configuration with spaces
   - `PUT /{companySlug}/space-configurations/{id}/spaces` - Replace included spaces (Manager+)

7. **`VenuePlatform.Web/Endpoints/BookingEndpoints.cs`** - Booking management (tenant-scoped):
   - `GET /{companySlug}/bookings` - List bookings
   - `GET /{companySlug}/bookings/{id}` - Get booking by ID
   - `GET /{companySlug}/bookings/{id}/details` - Get detailed booking info
   - `POST /{companySlug}/bookings` - Create booking (Manager+)
   - `POST /{companySlug}/bookings/with-spaces` - Create booking with spaces (Manager+)
   - `PUT /{companySlug}/bookings/{id}` - Update booking (Manager+)
   - `PUT /{companySlug}/bookings/{id}/spaces` - Replace booking spaces (Manager+)
   - `PUT /{companySlug}/bookings/{id}/with-spaces` - Update booking with spaces (Manager+)
   - `POST /{companySlug}/bookings/{id}/confirm` - Confirm booking (Manager+)
   - `DELETE /{companySlug}/bookings/{id}` - Cancel booking (Manager+)

8. **`VenuePlatform.Web/Endpoints/DevEndpoints.cs`** - Development-only endpoints (no auth):
   - `POST /dev/companies` - Create company
   - `POST /dev/users` - Create user
   - `POST /dev/memberships` - Assign user to company with role

**Changes to `VenuePlatform.Web/Program.cs`:**
- Reduced from ~2200 lines to ~85 lines
- Now acts as composition root only:
  - Service registrations (unchanged)
  - Middleware pipeline setup (unchanged order)
  - Swagger/OpenAPI configuration
  - Calls to endpoint extension methods
  - Database migration and seeding

**Behavior Preservation:**
- All routes remain identical (same paths, verbs, query params)
- All auth/role checks unchanged
- All validation logic unchanged
- All helper functions logic identical (moved to EndpointHelpers)
- Middleware order unchanged
- DI registrations unchanged
- Swagger behavior unchanged
- All responses identical

**Build Status:** Verified successful (0 warnings, 0 errors)

**Target Framework:** net10.0 confirmed

**Extension Method Pattern Used:**
```csharp
// Platform-level endpoints (non-tenant)
app.MapPlatformHealthEndpoints();
app.MapAuthEndpoints(builder.Configuration);

// Tenant-scoped endpoints
var tenantGroup = app.MapGroup("/{companySlug}");
tenantGroup.MapTenantHealthEndpoints();
tenantGroup.MapClientEndpoints();
tenantGroup.MapSpaceEndpoints();
tenantGroup.MapSpaceConfigurationEndpoints();
tenantGroup.MapBookingEndpoints();

// Dev-only endpoints (Development environment only)
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}
```

---

## 2026-03-03T23:51:19Z

**Summary:** Phase 7.0: Added Invoice + InvoiceItem and create-invoice-from-booking endpoints (KISS).

### Prompt Text

```
# Coder Mode — Phase 7.0: Invoicing foundation (Invoice + InvoiceItem) from Booking (KISS)

## Goal
Introduce minimal invoicing that can be generated from a Booking:
- Create an Invoice for a Booking (snapshot totals)
- Store line items (one per space)
- Read invoices

This is **not** payments, subscriptions, or billing automation yet.

---

## Hard constraints
- net10.0
- Minimal APIs only
- No Controllers/MVC
- No MediatR/CQRS/AutoMapper
- No new repositories/services unless already present patterns require them
- Keep changes minimal and explicit
- Tenant isolation via CompanyId must be enforced
- Avoid overengineering (no tax/discount engines, no PDF generation, no payment providers)

---

## Logging
Append this prompt to `docs/ai-prompts.md` with date/time placeholder + summary:
"Phase 7.0: Added Invoice + InvoiceItem and create-invoice-from-booking endpoints (KISS)."

---

# Business rules (KISS)

1) An Invoice belongs to a tenant (`CompanyId`).
2) An Invoice is linked to exactly one Booking (`BookingId`).
3) You can only generate an invoice if:
   - Booking exists in tenant
   - Booking is **Confirmed**
   - Booking is **not cancelled**
   - Booking has at least one Space attached
4) One invoice per booking:
   - If invoice already exists for that booking -> return 409 (or 400). Prefer **409 Conflict** with simple message.
5) Invoice is a snapshot:
   - Store `SubtotalAmount` (decimal 18,2)
   - Store line items at time of creation (do not recompute later)

No taxes, no discounts, no payments, no status lifecycle beyond Draft.

---

# Step 1 — BLL domain entities (minimal)

Create folder:
`VenuePlatform.BLL/Domain/Billing/`

Create:
`VenuePlatform.BLL/Domain/Billing/Invoice.cs`

Properties:
- Guid Id
- Guid CompanyId
- Guid BookingId
- DateTime CreatedUtc
- Guid CreatedByUserId
- decimal SubtotalAmount
- string Currency (3 letters, default "EUR")  // keep simple
- string Status (default "Draft")            // string to avoid enum churn for now

Create:
`VenuePlatform.BLL/Domain/Billing/InvoiceItem.cs`

Properties:
- Guid Id
- Guid CompanyId
- Guid InvoiceId
- string Description (max 200)
- int Quantity (>= 1)
- decimal UnitPrice (decimal 18,2)
- decimal LineTotal (decimal 18,2)

Rules:
- LineTotal = Quantity * UnitPrice
- SubtotalAmount = sum(LineTotal)
- Keep constructors minimal + basic validation (no negative values)
- No navigation props required for now

---

# Step 2 — DAL: DbSets + EF mapping + query filters

Edit:
`VenuePlatform.DAL/Persistence/ApplicationDbContext.cs`

Add DbSets:
- `DbSet<Invoice> Invoices`
- `DbSet<InvoiceItem> InvoiceItems`

Mapping:
## Invoice
- Table: "Invoices"
- Key: Id
- Required: CompanyId, BookingId, CreatedUtc, CreatedByUserId, SubtotalAmount, Currency, Status
- Currency max length 3
- Status max length 20
- Unique index on (CompanyId, BookingId)  // ensures one invoice per booking per tenant
- Tenant query filter like other tenant-scoped entities (`CompanyId == CurrentCompanyId`)

## InvoiceItem
- Table: "InvoiceItems"
- Key: Id
- Required: CompanyId, InvoiceId, Description, Quantity, UnitPrice, LineTotal
- Description max length 200
- Decimal precision 18,2 for money fields
- FK InvoiceId -> Invoices(Id) cascade delete
- Tenant query filter (`CompanyId == CurrentCompanyId`)

---

# Step 3 — Migration

Create migration:

- `dotnet ef migrations add AddInvoices -p VenuePlatform.DAL -s VenuePlatform.Web`
- `dotnet ef database update -p VenuePlatform.DAL -s VenuePlatform.Web`

---

# Step 4 — Contracts (DTOs)

Create:
`VenuePlatform.Contracts/Billing/InvoiceResponse.cs`

Fields:
- Guid Id
- Guid BookingId
- DateTime CreatedUtc
- Guid CreatedByUserId
- string Currency
- string Status
- decimal SubtotalAmount
- IReadOnlyList<InvoiceItemResponse> Items

Create:
`VenuePlatform.Contracts/Billing/InvoiceItemResponse.cs`

Fields:
- Guid Id
- string Description
- int Quantity
- decimal UnitPrice
- decimal LineTotal

No create DTO needed if invoice is always generated from booking.

---

# Step 5 — Web endpoints (Minimal APIs, in endpoint modules)

Add endpoints under tenant route group:

## A) POST /{companySlug}/bookings/{id:guid}/invoice
Access:
- Auth required
- Role: CompanyManager / CompanyAdmin / CompanyOwner

Flow:
1) Load booking (tenant-filtered)
   - 404 if not found
   - 400 if cancelled
   - 400 if Status != Confirmed
2) Ensure booking has spaces (BookingSpaces)
   - 400 if none
3) Check if invoice already exists for booking
   - if exists -> 409 Conflict
4) Load:
   - attached Spaces (Names) + their hourly rates used for pricing snapshot:
     - if booking has SpaceConfigurationId and override is set -> use override as unit price
     - else use each Space.HourlyRate
   - duration billed hours should match existing pricing logic:
     - 15-min ceiling rounding
5) Create Invoice (CreatedByUserId = current user sub)
6) Create InvoiceItems:
   - One item per attached space:
     - Description: "Space: {SpaceName}"
     - Quantity: billedMinutes (or billedHours?)  KISS choice:
       - Use **billedHours as quantity is awkward**
       - Prefer Quantity = 1 and UnitPrice = (spaceRate * billedHours) and LineTotal = same
       - Keep it simple and readable on invoices
7) Set Invoice.SubtotalAmount = sum(LineTotal)
8) SaveChanges
9) Return 201 Created with InvoiceResponse (including items)

Note:
- Do NOT change Booking.TotalAmount here. Invoice is a snapshot based on current booking.

## B) GET /{companySlug}/invoices
Access:
- Auth required
- Any membership

Return list of invoices (no items to keep payload small), ordered by CreatedUtc desc.
Create an `InvoiceListItemResponse` if needed; otherwise return InvoiceResponse with Items = empty list.
Prefer a dedicated list DTO for KISS payload size:

Create:
`VenuePlatform.Contracts/Billing/InvoiceListItemResponse.cs`
Fields: Id, BookingId, CreatedUtc, Status, Currency, SubtotalAmount

## C) GET /{companySlug}/invoices/{id:guid}
Access:
- Auth required
- Any membership

Return InvoiceResponse with items.
404 if not found.

Implementation notes:
- Use AsNoTracking()
- Avoid N+1 (query invoice + query items)

---

# Step 6 — Acceptance criteria

- Confirmed, non-cancelled booking with spaces can create invoice -> 201
- Cancelled booking -> 400
- Pending booking -> 400
- Booking without spaces -> 400
- Second invoice for same booking -> 409
- GET invoices returns tenant-only invoices
- GET invoice/{id} returns items
- Tenant isolation intact
- Build passes (0 warnings, 0 errors)

STOP AFTER IMPLEMENTING.
Do NOT implement:
- taxes/VAT
- discounts
- payments
- subscriptions
- PDF generation
- email sending
- invoice numbering scheme (we'll do next)
```

### Summary of Response

Phase 7.0: Added Invoice + InvoiceItem and create-invoice-from-booking endpoints (KISS).

**Implementation Complete:**

**New Domain Entities:**

1. **`VenuePlatform.BLL/Domain/Billing/Invoice.cs`**:
   - Properties: Id, CompanyId, BookingId, CreatedUtc, CreatedByUserId, SubtotalAmount, Currency, Status
   - Constructor with validation (no empty Guids, no negative amounts, currency must be 3 letters)
   - `SetSubtotalAmount()` method for calculated total

2. **`VenuePlatform.BLL/Domain/Billing/InvoiceItem.cs`**:
   - Properties: Id, CompanyId, InvoiceId, Description, Quantity, UnitPrice, LineTotal
   - Constructor with validation (Description max 200, Quantity >= 1, no negative prices)
   - Static `Create()` factory method that calculates LineTotal as Quantity * UnitPrice

**DAL Changes:**

**Changes to `VenuePlatform.DAL/Persistence/ApplicationDbContext.cs`:**
- Added `DbSet<Invoice> Invoices`
- Added `DbSet<InvoiceItem> InvoiceItems`
- Added Invoice EF configuration:
  - Table "Invoices" with required fields
  - Unique index on (CompanyId, BookingId) for one-invoice-per-booking constraint
  - Tenant query filter on CompanyId
  - Decimal precision 18,2 for SubtotalAmount
- Added InvoiceItem EF configuration:
  - Table "InvoiceItems" with required fields
  - FK to Invoices with Cascade delete
  - Tenant query filter on CompanyId
  - Decimal precision 18,2 for money fields

**Migration Files Created:**
- `20260303235251_AddInvoices.cs` - Creates Invoices and InvoiceItems tables with indexes
- `20260303235251_AddInvoices.Designer.cs` - Migration designer file
- Updated `ApplicationDbContextModelSnapshot.cs` - Added Invoice and InvoiceItem entities

**New Contracts/DTOs:**

1. **`VenuePlatform.Contracts/Billing/InvoiceItemResponse.cs`**:
   - Record with Id, Description, Quantity, UnitPrice, LineTotal

2. **`VenuePlatform.Contracts/Billing/InvoiceResponse.cs`**:
   - Record with Id, BookingId, CreatedUtc, CreatedByUserId, Currency, Status, SubtotalAmount, Items

3. **`VenuePlatform.Contracts/Billing/InvoiceListItemResponse.cs`**:
   - Lightweight record for list view: Id, BookingId, CreatedUtc, Status, Currency, SubtotalAmount

**New Web Endpoint Module:**

**`VenuePlatform.Web/Endpoints/InvoiceEndpoints.cs`**:
1. `POST /{companySlug}/bookings/{id:guid}/invoice` - Create invoice from booking:
   - Requires authentication + Manager/Admin/Owner role
   - Validates booking exists (404 if not found)
   - Validates booking is not cancelled (400)
   - Validates booking is Confirmed (400)
   - Validates booking has spaces (400)
   - Checks for existing invoice (409 Conflict if exists)
   - Calculates billed duration using 15-min ceiling (same as booking pricing)
   - Loads spaces with hourly rates
   - Checks for SpaceConfiguration HourlyRateOverride
   - Creates Invoice with Draft status
   - Creates one InvoiceItem per space with calculated line total
   - Returns 201 Created with InvoiceResponse

2. `GET /{companySlug}/invoices` - List invoices:
   - Requires authentication + any membership
   - Returns tenant-filtered invoices ordered by CreatedUtc desc
   - Returns InvoiceListItemResponse (lightweight, no items)

3. `GET /{companySlug}/invoices/{id:guid}` - Get invoice details:
   - Requires authentication + any membership
   - Returns InvoiceResponse with full item list
   - Uses AsNoTracking() for read efficiency

**Changes to `VenuePlatform.Web/Program.cs`:**
- Added `tenantGroup.MapInvoiceEndpoints()` to register invoice endpoints

**Build Status:** Verified successful (0 warnings, 0 errors)

**Target Framework:** net10.0 confirmed

**Business Rules Enforced:**
- One invoice per booking per tenant (enforced by unique database index)
- Invoice only created for confirmed, non-cancelled bookings with spaces
- Invoice is a snapshot (prices calculated at creation time, not recomputed later)
- Line items: one per space with Description "Space: {Name}", Quantity=1, UnitPrice=rate*billedHours
- Tenant isolation enforced via query filters and explicit CompanyId checks

**NOT Implemented (as per spec):**
- Taxes/VAT
- Discounts
- Payments
- Subscriptions
- PDF generation
- Email sending
- Invoice numbering scheme

---
