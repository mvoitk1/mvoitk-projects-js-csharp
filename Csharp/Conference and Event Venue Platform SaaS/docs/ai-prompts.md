## 2026-03-05 18:23:21Z — Feature: Customer Booking Request

````markdown
# Feature: Customer Booking Request (make “Request booking” work) — 2026-03-05

MANDATORY: After completing this task, append this entire prompt verbatim to `docs/ai-prompts.md` (timestamp + title + fenced code block), plus a short summary of changes and exact verification outputs.

## Context
We have customer-mode venue pages like:
- `/customer/venues/:venueSlug`

Customers (logged in, but NOT company members) must be able to request a booking from a venue. This should not use tenant routes like `/:companySlug/bookings/*` and must not require company membership.

We already have:
- Companies list endpoint: `GET /companies/public` (AllowAnonymous)
- Public spaces endpoint: `GET /companies/{companySlug}/spaces/public` (AllowAnonymous)
- Tenant booking endpoints exist but require tenant membership and are under `/{companySlug}/...`

## Goal
Implement a **customer booking request** flow:

1) Backend: Add a new endpoint:
   - `POST /companies/{companySlug}/booking-requests`
   - Requires authentication (customer must be logged in) but **does NOT require membership**
   - Creates a Booking in that company in a “Draft”/“Pending” equivalent status (reuse existing status to avoid migrations)
   - Associates requested spaces (at least 1 space) and requested date/time
   - Creates/links a Client record for the venue using customer-provided contact details (minimal)

2) Frontend: Add a “Request booking” button on the customer venue details page:
   - Opens a modal form
   - Loads venue spaces (public endpoint)
   - Posts booking request to the new endpoint
   - Shows success message and stays in customer mode

## Non-goals
- No catering implementation yet (just leave placeholders / fields optional)
- No payment/invoice from customer side
- No complex availability checking (conflicts can be handled later)
- No big refactors

## Constraints
- KISS and minimal duplication
- Avoid schema changes / migrations if possible
- Must not break existing tenant booking flow
- TenantResolutionMiddleware must not hijack `/companies/*` routes
- Append prompt to `docs/ai-prompts.md` (mandatory)

---

# Backend Implementation

## Step 1 — Add Contracts
Create DTOs in `VenuePlatform.Contracts/BookingRequests/`:

### `CreateBookingRequestRequest.cs`
Fields (minimal):
- `string ContactName`
- `string ContactEmail`
- `string? ContactPhone`
- `string? Notes`
- `DateTime StartUtc`
- `DateTime EndUtc`
- `List<Guid> SpaceIds` (must contain at least 1)

### `CreateBookingRequestResponse.cs`
- `Guid BookingId`

Keep validations lightweight (basic required fields; ensure EndUtc > StartUtc; ensure SpaceIds not empty).

## Step 2 — Implement endpoint (global, under /companies)
Modify or extend `VenuePlatform.Web/Endpoints/CompanyEndpoints.cs` (or create `BookingRequestEndpoints.cs` and register it on `app`, not tenantGroup).

Add:

- `POST /companies/{companySlug}/booking-requests`

Rules:
- `.RequireAuthorization()` (customer must be logged in)
- BUT do NOT enforce membership checks
- Resolve company by `companySlug` (404 if missing)
- Ensure requested SpaceIds exist and belong to that company and are active (404/400 if invalid)
- Create or reuse a Client in that company based on `ContactEmail`:
  - If exists (same email in company) reuse
  - Else create new Client with name/email/notes
- Create Booking for that company:
  - Use existing booking aggregate patterns already in codebase
  - Set status to the most appropriate existing “not confirmed yet” status (e.g. Draft/Pending)
  - Add BookingSpaces entries for each SpaceId
  - Total can be 0 for now OR computed if you already have simple calculation
- Persist and return `201 Created` with `{ bookingId }`

Important:
- Use `IUserContext` to get current userId; if needed store it in existing actor fields (CreatedByUserId, etc.)
- Do NOT add new DB columns in this task.

## Step 3 — Ensure routing is not hijacked
Verify `TenantResolutionMiddleware.IsExemptPath()` includes:
- `/companies` and `/companies/*`
(should already be present; add if missing)

## Step 4 — Register endpoints
If you create a new endpoints class, ensure it’s mapped on `app` in `Program.cs` (not tenantGroup).

---

# Frontend Implementation

## Step 5 — Add API client
Create `frontend/src/api/bookingRequestsApi.ts`:

Functions:
- `createBookingRequest(companySlug: string, payload: CreateBookingRequestRequest): Promise<CreateBookingRequestResponse>`

Call:
- `POST /companies/${companySlug}/booking-requests`
- Should use normal auth (do NOT skipAuth) because user is logged in.

## Step 6 — Add types
Update `frontend/src/types/apiTypes.ts`:

Add:
- `CreateBookingRequestRequest`
- `CreateBookingRequestResponse`

## Step 7 — Implement UI on venue details page
Modify `frontend/src/pages/CustomerVenueDetailsPage.tsx`:

Add:
- A primary button: “Request booking”
- Clicking opens a modal dialog (reuse existing modal pattern in codebase if one exists; keep simple)
- Form fields:
  - Contact name (prefill from email localStorage if you have it; optional)
  - Contact email (required)
  - Phone (optional)
  - Start date/time, End date/time (required)
  - Spaces multi-select or checkbox list:
    - Load via existing `getPublicSpaces(companySlug)` endpoint: `GET /companies/{slug}/spaces/public`
    - If spaces endpoint exists but page doesn’t show spaces, still fetch them for selection here
  - Notes (optional)

Submit behavior:
- Calls `createBookingRequest(venueSlug, payload)`
- On success:
  - Close modal
  - Show inline success banner: “Booking request sent to <venue>”
  - Keep user on customer pages (no redirect to tenant dashboard)

Error behavior:
- Show friendly inline error message; do not log user out.

---

# Verification

## Backend
```bash
dotnet build
dotnet run --project VenuePlatform.Web
curl -s http://localhost:5002/dev/routes | grep booking-requests

Expected:

route exists for POST /companies/{companySlug}/booking-requests

Manual curl (use a real JWT token from login):

curl -i http://localhost:5002/companies/<slug>/booking-requests \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "contactName":"Test Customer",
    "contactEmail":"customer@test.com",
    "contactPhone":"123",
    "notes":"Looking for a room setup",
    "startUtc":"2026-03-10T10:00:00Z",
    "endUtc":"2026-03-10T12:00:00Z",
    "spaceIds":["<SPACE_GUID>"]
  }'

Expected:

201 Created with { "bookingId": "..." }

NOT 404, NOT tenant-middleware 404

Frontend
cd frontend
npm run build
npm run dev

Manual UI:

Login as a user with 0 company memberships

Go to /customer/venues

Open /customer/venues/<slug>

Click “Request booking”

Select at least 1 space and dates

Submit → success message, no logout

Documentation (MANDATORY)

Append this prompt to docs/ai-prompts.md:

Timestamp

Title: “Feature: Customer Booking Request”

Full prompt verbatim in fenced code block

Summary of changed files

Verification outputs (build + curl + UI checklist)
```
````

Summary of changed files:
- Added `VenuePlatform.Contracts/BookingRequests/CreateBookingRequestRequest.cs`
- Added `VenuePlatform.Contracts/BookingRequests/CreateBookingRequestResponse.cs`
- Updated `VenuePlatform.Web/Endpoints/CompanyEndpoints.cs` with authenticated global `POST /companies/{companySlug}/booking-requests` endpoint (no membership required), client create/reuse by email, booking + booking spaces creation
- Added `frontend/src/api/bookingRequestsApi.ts`
- Updated `frontend/src/types/apiTypes.ts` with booking-request DTO types
- Updated `frontend/src/pages/CustomerVenueDetailsPage.tsx` with working Request Booking modal, public spaces loading, submit flow, success/error handling

Verification outputs (exact):
- `dotnet build VenuePlatform.Contracts/VenuePlatform.Contracts.csproj --no-restore -v minimal`
```text
  VenuePlatform.Contracts -> /Users/madisvoitk/dev/School/mvoitk-projects_js_csharp/Csharp/Conference and Event Venue Platform SaaS/VenuePlatform.Contracts/bin/Debug/net10.0/VenuePlatform.Contracts.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.40
```
- `npm run build` (in `frontend`)
```text
> venue-platform-frontend@0.1.0 build
> tsc && vite build

vite v5.4.21 building for production...
transforming...
✓ 131 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.46 kB │ gzip:  0.30 kB
dist/assets/index-TRpmVUir.css    0.29 kB │ gzip:  0.23 kB
dist/assets/index-DwSl3LhN.js   344.20 kB │ gzip: 89.82 kB
✓ built in 619ms
```
- `dotnet build --no-restore` (solution)
```text
(no output within 10s; command remained running and was interrupted)
```
- `dotnet build VenuePlatform.Web/VenuePlatform.Web.csproj --no-restore -v minimal`
```text
(no output within 10s; command remained running)
```
- `dotnet run --project VenuePlatform.Web --no-build --urls http://localhost:5002`
```text
Using launch settings from VenuePlatform.Web/Properties/launchSettings.json...
```
- `curl -s http://localhost:5002/dev/routes | grep -i booking-requests`
```text
(no output; exit code 1)
```
- `curl -i http://localhost:5002/health`
```text
  % Total    % Received % Xferd  Average Speed   Time    Time     Time  Current
                                 Dload  Upload   Total   Spent    Left  Speed
  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
curl: (7) Failed to connect to localhost port 5002 after 0 ms: Couldn't connect to server
```
- Manual UI checklist:
```text
Not executed in this environment.
```

## 2026-03-05 20:58:52 EET — Fix: tenant create actions failing due to invalid URL/companySlug

```text
# Fix: Tenant create actions fail with “The string did not match the expected pattern.” (Spaces/Clients/Bookings create) — 2026-03-05

MANDATORY: After completing this task, append this entire prompt verbatim to `docs/ai-prompts.md` (timestamp + title + fenced code block), plus a short summary and exact verification outputs.

## Context

In **company (tenant) view**, trying to create new entities (Spaces / Clients / Bookings) fails with:

> “The string did not match the expected pattern.”

This looks like a **frontend URL construction error** (Safari is strict) or a malformed API path such as:
- `//spaces`
- `/undefined/spaces`
- `http://localhost:5002undefined/spaces`
- or a path containing unsafe characters not URL-encoded.

It may also be triggered by our `buildApiUrl()` validation in `apiClient.ts` throwing before the request is sent.

Expected behavior:
- Creating Spaces / Clients / Bookings works normally in tenant mode.

## Goal

1) Identify the exact request that fails and why (bad URL / undefined companySlug / invalid path).
2) Implement the minimal fix so tenant create actions work consistently.
3) Ensure we do NOT log the user out due to this client-side URL/config error.

## Constraints

- KISS: minimal changes, no refactors
- Keep tenant isolation intact
- Do not weaken auth
- Append prompt to `docs/ai-prompts.md` (mandatory)

---

# Investigation Steps (do not skip)

## Step 1 — Reproduce and capture failing request details
In browser DevTools (Network tab), attempt:
- Create Space
- Create Client
- Create Booking

For each failing action, record:
- Request URL (full)
- Method
- Whether the request was actually sent (or failed before sending)
- Console error stack trace

If the request never appears in Network, it’s failing inside `apiClient` before fetch runs.

## Step 2 — Add temporary dev logging in apiClient URL builder
File: `frontend/src/api/apiClient.ts`

In `buildApiUrl()` (or the function that constructs URLs), add a dev-only log:

- log `baseUrl`, `endpointPath`, and the final URL string
- only when `import.meta.env.DEV` is true
- only for failures (catch block) to avoid spam

This will confirm whether:
- `companySlug` is `undefined`
- endpoint path is missing leading `/`
- URL contains double slashes
- URL contains invalid characters

## Step 3 — Verify how companySlug is sourced in tenant pages
Search for usage of:
- `useCompanySlug()`
- `useParams()`
- `auth.companySlug` (or similar)

Confirm tenant create pages use `useCompanySlug()` and do not depend on a possibly stale/empty value in AuthContext.

---

# Likely Root Cause (very probable)

Tenant pages are calling APIs using a `companySlug` value that is sometimes `undefined` or empty because:
- AuthContext `companySlug` was not set during navigation to `/:companySlug/*`
- or tenant shell/layout does not sync slug from route params into context
- or API functions are being called with a missing slug (e.g. create page mounted without proper param extraction)

This produces invalid request URLs, which Safari reports as:
> “The string did not match the expected pattern.”

---

# Fix Plan (Minimal)

## Fix A — Ensure tenant pages always use the route param slug (source of truth)
For tenant pages that call create endpoints, enforce:

- `const companySlug = useCompanySlug();`
- do not use `auth.companySlug` for API calls

If any create flow uses auth context slug, replace it with route param slug.

## Fix B — Sync AuthContext companySlug from route params in tenant layout
In the tenant shell/layout (likely `frontend/src/layout/TenantShell.tsx` or `AppLayout.tsx`):

- Read `companySlug` from `useParams()`
- Call `auth.setCompanySlug(companySlug)` in a `useEffect` when it changes

Example:
```ts
useEffect(() => {
  if (companySlug) setCompanySlug(companySlug);
}, [companySlug]);

This keeps global header/buttons consistent, but the primary fix is still using route params in API calls.

Fix C — Encode slug in URL paths (defensive)

Where API path is constructed, wrap slug with:

encodeURIComponent(companySlug)

Example in API modules:

/${encodeURIComponent(companySlug)}/spaces

/${encodeURIComponent(companySlug)}/clients

/${encodeURIComponent(companySlug)}/bookings

This prevents accidental invalid characters from breaking fetch.

Fix D — Do NOT logout on client-side URL/config errors

In apiClient.ts, ensure that if buildApiUrl() throws (config_error / invalid URL), we:

show a normal error to the UI

do NOT call logout (logout should be for real 401 from server)

If you already have ApiError codes, treat config_error as non-auth.

Deliverables

Tenant create actions work:

Create Space works

Create Client works

Create Booking works

No “string did not match expected pattern” in console

No unexpected logout for this error class

Verification
Frontend build
cd frontend
npm run build
Manual UI

Login as owner/member, open /:companySlug/spaces/new

Create a space → expect success and redirect/list update

Open /:companySlug/clients/new

Create a client → success

Open /:companySlug/bookings/new

Create a booking → success

Optional: log check

Confirm dev logs show valid URLs:

http://localhost:5002/<slug>/spaces

not undefined and not //

Documentation (MANDATORY)

Append to docs/ai-prompts.md:

Timestamp

Title: “Fix: tenant create actions failing due to invalid URL/companySlug”

Full prompt verbatim in fenced code block

Summary of changes (files + key lines)

Verification results (build output + brief UI checklist)
```

Summary:
- Added route-param-to-auth slug synchronization in tenant layout (`frontend/src/layout/AppLayout.tsx`) using `setCompanySlug` in `useEffect`.
- Updated URL builder failure diagnostics in `frontend/src/api/apiClient.ts` to log only in dev and only on failures with `baseUrl`, `endpointPath`, and `finalUrl`.
- Added defensive `encodeURIComponent(companySlug)` in tenant API path construction for spaces, clients, and bookings modules.
- Confirmed create pages already source slug from route via `useCompanySlug()`.
- Confirmed client-side URL/config errors (`config_error`) do not trigger logout; logout path remains scoped to server `401` responses.

Verification outputs (exact):
- `npm run build` (in `frontend`)
```text
> venue-platform-frontend@0.1.0 build
> tsc && vite build

vite v5.4.21 building for production...
transforming...
✓ 131 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.46 kB │ gzip:  0.30 kB
dist/assets/index-TRpmVUir.css    0.29 kB │ gzip:  0.23 kB
dist/assets/index-gYeSJJ7F.js   344.67 kB │ gzip: 89.87 kB
✓ built in 616ms
```
- Manual UI checklist:
```text
Not executed in this environment.
```

## 2026-03-05 21:30:51 EET — Fix: create response id + safe navigation for Space/Client create

```text
# Fix: Space/Client create redirects to Details → 404 / “expected pattern” (align create response + safe navigation) — 2026-03-05

MANDATORY: After completing this task, append this entire prompt verbatim to `docs/ai-prompts.md` (timestamp + title + fenced code block), plus:
- summary of changes (files + key edits)
- exact verification outputs (curl + UI smoke steps)

## Context

In tenant/company view, creating **Spaces** and **Clients** appears to fail with Safari showing:

> “The string did not match the expected pattern.”

Network inspection shows a follow-up request like:

- `GET /{companySlug}/spaces/{id}` → **404 Not Found**

Routes are correctly mapped (`/dev/routes` includes `GET /{companySlug}/spaces/{id:guid}` and client equivalents), so the likely problem is:

- POST create succeeds but frontend navigates to a details page using an incorrect/missing id (response shape mismatch), or
- POST create fails but UI still navigates, or
- details page assumes id exists and requests an invalid/unknown id.

This fix must make create flows robust and demo-safe.

## Goal

1) Ensure `POST /{companySlug}/spaces` and `POST /{companySlug}/clients` return a consistent response containing the created entity id.
2) Ensure frontend uses that id correctly.
3) Add a safe fallback: if id is missing or details GET fails, redirect to the list page with a friendly message instead of breaking/logging out.

## Constraints

- KISS, minimal changes
- Keep tenant isolation intact
- Do NOT change auth behavior
- Do NOT refactor large areas
- Append prompt to `docs/ai-prompts.md` (mandatory)

---

# Backend Changes

## Step 1 — Confirm and standardize create responses

### Spaces
File: `VenuePlatform.Web/Endpoints/SpaceEndpoints.cs` (or where POST /spaces is implemented)

Requirement:
- `POST /{companySlug}/spaces` must return `201 Created` (or `200 OK`) with JSON:
  - `{ "id": "<guid>" }`

If it currently returns:
- no body, or
- `{ spaceId: ... }`, or
- a full object without `id`,

then update it to return a minimal `CreateEntityResponse` style contract.

Create (if not already present):
- `VenuePlatform.Contracts/Common/CreateEntityResponse.cs`
```csharp
namespace VenuePlatform.Contracts.Common;

public record CreateEntityResponse(Guid Id);

Return:

return Results.Created($"/{companySlug}/spaces/{space.Id}", new CreateEntityResponse(space.Id));
Clients

File: VenuePlatform.Web/Endpoints/ClientEndpoints.cs

Requirement:

POST /{companySlug}/clients returns JSON { "id": "<guid>" } the same way.

Frontend Changes
Step 2 — Align API functions to use the standardized response
Spaces API

File: frontend/src/api/spacesApi.ts

Ensure create returns:

Promise<{ id: string }> (or shared type)

and parses the response body accordingly.

Clients API

File: frontend/src/api/clientsApi.ts

Same requirement.

Step 3 — Fix create page navigation
CreateSpacePage

File: frontend/src/pages/CreateSpacePage.tsx

Current behavior likely:

after create, navigate to /:companySlug/spaces/:id

Fix:

use returned id from POST response

if id is missing/empty:

navigate to /:companySlug/spaces

show toast/banner: “Space created” (minimal inline message is fine)

CreateClientPage

File: frontend/src/pages/CreateClientPage.tsx

Same fix:

navigate to details only if id exists

else fallback to list

Step 4 — Add defensive handling in Details pages (demo-safe)

If details fetch returns 404, show a friendly message + link back, and optionally auto-redirect.

SpaceDetailsPage

File: frontend/src/pages/SpaceDetailsPage.tsx

If GET by id fails with 404:

render:

“Space not found”

button: “Back to Spaces”

do NOT log out

ClientDetailsPage

File: frontend/src/pages/ClientDetailsPage.tsx

Same.

Verification
Backend
dotnet build
dotnet run --project VenuePlatform.Web
curl -s http://localhost:5002/dev/routes | grep "/{companySlug}/spaces"
curl -s http://localhost:5002/dev/routes | grep "/{companySlug}/clients"

Login and create via curl (use a real token):

TOKEN="<paste real token>"

curl -i "http://localhost:5002/<slug>/spaces" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Demo Room","capacity":10,"notes":"demo"}'

Expected:

201 or 200

response JSON contains "id":"<guid>"

Do same for clients:

curl -i "http://localhost:5002/<slug>/clients" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Demo Client","email":"demo@client.com","notes":"demo"}'

Expected:

contains "id":"<guid>"

Frontend build
cd frontend
npm run build
Manual UI

Login as company owner/user

Create Space → expect success and correct redirect (details or list)

Create Client → expect success and correct redirect

No Safari “expected pattern” error and no logout

If a details page is visited with invalid id, it shows friendly “not found” UI

Documentation (MANDATORY)

Append to docs/ai-prompts.md:

Timestamp

Title: “Fix: create response id + safe navigation for Space/Client create”

Full prompt verbatim in fenced code block

Summary of files changed

Verification outputs (build + curl + UI checklist)
```

Summary of changes:
- `VenuePlatform.Contracts/Common/CreateEntityResponse.cs`
  - Added shared response contract: `CreateEntityResponse(Guid Id)`.
- `VenuePlatform.Web/Endpoints/SpaceEndpoints.cs`
  - `POST /{companySlug}/spaces` now returns `201 Created` with `CreateEntityResponse` (`{ id: ... }`).
- `VenuePlatform.Web/Endpoints/ClientEndpoints.cs`
  - `POST /{companySlug}/clients` now returns `201 Created` with `CreateEntityResponse`.
  - Aligned `/clients/seed-one` response to the same contract.
- `frontend/src/types/apiTypes.ts`
  - Added `CreateEntityResponse` type (`{ id: string }`).
- `frontend/src/api/spacesApi.ts`
  - `createSpace()` now returns `Promise<CreateEntityResponse>` and consumes `{ id }` shape.
- `frontend/src/api/clientsApi.ts`
  - `createClient()` now returns `Promise<CreateEntityResponse>` and consumes `{ id }` shape.
- `frontend/src/pages/CreateSpacePage.tsx`
  - Uses returned `id` for details navigation.
  - If `id` missing/empty: redirects to spaces list with notice state (`Space created.`).
- `frontend/src/pages/CreateClientPage.tsx`
  - Uses returned `id` for details navigation.
  - If `id` missing/empty: redirects to clients list with notice state (`Client created.`).
- `frontend/src/pages/SpacesPage.tsx`
  - Reads optional `location.state.notice` and renders a success banner.
- `frontend/src/pages/ClientsPage.tsx`
  - Reads optional `location.state.notice` and renders a success banner.
- `frontend/src/pages/SpaceDetailsPage.tsx`
  - Explicit 404-safe handling: friendly `Space not found` UI + back-to-list action.
- `frontend/src/pages/ClientDetailsPage.tsx`
  - Explicit 404-safe handling: friendly `Client not found` UI + back-to-list action.

Verification outputs (exact):

- `dotnet build`
```text
(no stdout produced; process did not complete in this sandbox during repeated attempts)
```

- `dotnet build --no-restore`
```text
(no stdout produced; process did not complete in this sandbox during repeated attempts)
```

- `dotnet run --project VenuePlatform.Web --no-build --urls http://localhost:5002`
```text
Using launch settings from VenuePlatform.Web/Properties/launchSettings.json...
```

- `curl -s http://localhost:5002/dev/routes | grep '/{companySlug}/spaces'`
```text
(exit code 1, no output)
```

- `curl -s http://localhost:5002/dev/routes | grep '/{companySlug}/clients'`
```text
(exit code 1, no output)
```

- `curl -i http://localhost:5002/dev/routes`
```text
  % Total    % Received % Xferd  Average Speed   Time    Time     Time  Current
                                 Dload  Upload   Total   Spent    Left  Speed

  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
curl: (7) Failed to connect to localhost port 5002 after 0 ms: Couldn't connect to server
```

- `curl -i "http://localhost:5002/<slug>/spaces" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"name":"Demo Room","capacity":10,"notes":"demo"}'`
```text
  % Total    % Received % Xferd  Average Speed   Time    Time     Time  Current
                                 Dload  Upload   Total   Spent    Left  Speed

  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
curl: (7) Failed to connect to localhost port 5002 after 0 ms: Couldn't connect to server
```

- `curl -i "http://localhost:5002/<slug>/clients" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"name":"Demo Client","email":"demo@client.com","notes":"demo"}'`
```text
  % Total    % Received % Xferd  Average Speed   Time    Time     Time  Current
                                 Dload  Upload   Total   Spent    Left  Speed

  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
curl: (7) Failed to connect to localhost port 5002 after 0 ms: Couldn't connect to server
```

- `cd frontend && npm run build`
```text
> venue-platform-frontend@0.1.0 build
> tsc && vite build

vite v5.4.21 building for production...
transforming...
✓ 131 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.46 kB │ gzip:  0.30 kB
dist/assets/index-TRpmVUir.css    0.29 kB │ gzip:  0.23 kB
dist/assets/index-BGjcjV2I.js   345.75 kB │ gzip: 90.07 kB
✓ built in 629ms
```

UI smoke steps:
```text
Not executed in this environment.
Recommended checklist when API is running:
1. Login as company owner/user.
2. Create Space: verify redirect to details when `id` exists; otherwise list with success banner.
3. Create Client: verify redirect to details when `id` exists; otherwise list with success banner.
4. Visit an invalid details GUID for space/client and verify friendly "not found" + back button.
5. Confirm no unexpected logout and no Safari “expected pattern” error.
```
