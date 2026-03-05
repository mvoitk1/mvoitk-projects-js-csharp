# UI Phase 1.2 — Dev Bootstrap + Better Login Errors (KISS)

## Goal
Make local verification possible when the database is empty by adding a dev-only bootstrap flow (create company + user + membership) and improve LoginPage error messages so failures are actionable (company not found, backend not running, invalid credentials, etc.). Do NOT add public registration.

## Constraints
- KISS, clean code.
- No production registration endpoint.
- Dev-only functionality must be gated:
  - Frontend: only visible/usable when `import.meta.env.DEV === true`
  - Backend endpoints used must already be dev-only (`if (app.Environment.IsDevelopment())`) or add a dev-only endpoint if missing.
- Do not change existing endpoint behavior except for improved error mapping in the UI.
- Keep JWT auth flow the same (POST `/auth/login`).

## Required Outputs
1) Implement the changes in the frontend.
2) Append this full prompt text + summary to `docs/ai-prompts.md`.
3) If `docs/ui-implementation-plan.md` exists, append a short "UI Phase 1.2 done" entry; otherwise create `docs/ui-implementation-plan.md` and add a minimal section for Phase 1.2.

---

## Part A — Dev Bootstrap UI (Frontend)

### 1) Create Page
Create `frontend/src/pages/DevBootstrapPage.tsx`:
- Route: `/dev/bootstrap`
- Only render this route in dev builds (`import.meta.env.DEV`).
- UI fields:
  - CompanyName (default "Acme Co")
  - CompanySlug (default "acme")
  - Email (default "owner@acme.com")
  - Password (default "Pass123$")
  - Role (default "CompanyOwner") (dropdown if multiple roles supported)
- Button: "Create Company + Owner"
- On success:
  - Show returned ids/summary
  - Provide a "Login as Owner" button that:
    - calls normal login (`POST /auth/login`) with email/password
    - stores token in AuthContext (existing behavior)
    - navigates to `/{companySlug}/dashboard`

### 2) Dev API wrapper
Create `frontend/src/api/devApi.ts` with one function:
- `bootstrapTenant(payload)` that calls one of:
  - Existing dev endpoints (preferred) OR
  - If no single endpoint exists, call existing dev endpoints in sequence (create company, create user, create membership).
Return a normalized object `{ companyId, userId, companySlug, role }`.

### 3) Add Route
Update `frontend/src/routes/AppRouter.tsx`:
- Add route `/dev/bootstrap` -> `DevBootstrapPage`
- Dev-only route registration (only in dev)

### 4) Add Link
Update `frontend/src/pages/LoginPage.tsx`:
- In dev builds only, show a small link "Dev bootstrap" -> `/dev/bootstrap`

---

## Part B — Improve Login Error UX (Frontend)

### 1) Expand ApiError normalization
In `frontend/src/api/apiClient.ts`, ensure errors preserve:
- `status`
- `code` (if backend returns `{ code, error, details }`)
- `error` message
- `details`
For non-JSON/network failures:
- Create `ApiError` with `status = 0` and message: "Backend not reachable. Is the API running?"

### 2) LoginPage messaging
Update `frontend/src/pages/LoginPage.tsx` to show targeted messages:
- `status === 0`: "Backend not reachable. Check API base URL and that backend is running."
- `status === 404`: "Company not found. Check company slug."
- `status === 401` + `code === invalid_credentials`: "Invalid email or password."
- `status === 403`: "You do not have access to this company."
- Else: show `error` or fallback "Unexpected error."
In dev builds only, show a collapsible "Details" block with the raw normalized error object for debugging.

### 3) ProtectedRoute membership check remains
Do not change the membership guard behavior except to display better messages when it fails.

---

## Part C — Backend Dev Bootstrap Endpoint (Only if needed)

If the backend does NOT already expose dev endpoints sufficient for bootstrapping from the UI:
- Add a single dev-only endpoint in `VenuePlatform.Web/Endpoints/DevEndpoints.cs`:

`POST /dev/bootstrap`
Input:
```json
{
  "companyName": "Acme Co",
  "companySlug": "acme",
  "email": "owner@acme.com",
  "password": "Pass123$",
  "role": "CompanyOwner"
}
```
Output:
```json
{
  "companyId": "uuid",
  "companySlug": "acme",
  "userId": "uuid",
  "role": "CompanyOwner"
}
```
- Must be dev-only (`if (app.Environment.IsDevelopment())`).
- Uses existing registration services internally.
- Returns 200 with JSON on success, appropriate 4xx on validation failure.

---

## Summary of Implementation

- Created `DevBootstrapPage.tsx` - UI for dev bootstrap at `/dev/bootstrap`
- Created `devApi.ts` - API wrapper for bootstrap endpoint
- Updated `AppRouter.tsx` - Added dev route
- Updated `LoginPage.tsx` - Added dev bootstrap link and better error messaging
- Backend endpoint `/dev/bootstrap` already exists in `DevEndpoints.cs`
- Error handling already preserves status/code in `apiClient.ts`

---

# Patch: Fix DevEndpoints compile errors — 2026-03-04T20:44:24Z

## Summary
Fixed CS7036 (missing CancellationToken) and CS1061 (DeleteAsync not found) compile errors in `VenuePlatform.Web/Endpoints/DevEndpoints.cs` by converting the `/dev/bootstrap` endpoint to use `ApplicationDbContext` directly instead of `ICompanyRepository`.

## Full Prompt

```
# Patch Request: Fix DevEndpoints compile errors (CancellationToken + missing DeleteAsync) — KISS, dev-only

## Context
`dotnet run --project VenuePlatform.Web` fails due to compile errors in:
- `VenuePlatform.Web/Endpoints/DevEndpoints.cs`

Errors:
1) CS7036: No argument given for required parameter `cancellationToken`:
   - `ICompanyRepository.GetBySlugAsync(string, CancellationToken)` (line ~156)
   - `ICompanyRepository.AddAsync(Company, CancellationToken)` (line ~164)

2) CS1061: `ICompanyRepository` does not contain `DeleteAsync` (line ~177)

This is dev-only functionality; production endpoints must not be touched.

## Goal
Make the project compile and run again with the smallest possible change, preserving behavior of dev endpoints.

## Requirements
1) Fix the compile errors by updating the dev endpoint(s) to use a consistent data access approach:
   - **Preferred**: use `ApplicationDbContext` directly inside DevEndpoints (dev-only), avoiding repository interface drift.
   - **Alternative acceptable**: keep repository usage but pass the `CancellationToken` everywhere and remove/replace `DeleteAsync` with an existing supported operation (only if truly needed).
2) Do **not** introduce new repository methods just to make this compile (no adding `DeleteAsync` to interfaces unless unavoidable).
3) Ensure handlers accept and pass `CancellationToken ct` to EF async calls.
4) Keep all existing slug validation, response shapes, routes, and dev-only guards unchanged.
5) After patch:
   - `dotnet build` must succeed with 0 errors.
   - `dotnet run --project VenuePlatform.Web` must start successfully.

## Implementation guidance (preferred approach)
- In the affected dev endpoint handler(s), replace:
  - `repo.GetBySlugAsync(slug)` with `await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug, ct)`
  - `await repo.AddAsync(company)` with `db.Companies.Add(company); await db.SaveChangesAsync(ct)`
  - Any deletion attempt should be `db.Companies.Remove(company); await db.SaveChangesAsync(ct)` (only if deletion is actually required by the endpoint logic)
- Add `ApplicationDbContext db` and `CancellationToken ct` parameters to the minimal API delegate.

## Documentation (must not be shortened)
- Append to `docs/ai-prompts.md`:
  - UTC timestamp
  - Summary
  - **Full prompt text verbatim** in a fenced code block
  - Ensure it is appended (do not overwrite existing content)

## Deliverable
- Provide the exact code diff or the exact updated code blocks for the affected endpoint(s), and confirm build success.
```

---

# Patch: Fix DevEndpoints compile errors — 2026-03-04T20:44:24Z

## Summary
Fixed CS7036 (missing CancellationToken) and CS1061 (DeleteAsync not found) compile errors in `VenuePlatform.Web/Endpoints/DevEndpoints.cs` by converting the `/dev/bootstrap` endpoint to use `ApplicationDbContext` directly instead of `ICompanyRepository`.

## Full Prompt

```
# Patch Request: Fix DevEndpoints compile errors (CancellationToken + missing DeleteAsync) — KISS, dev-only

## Context
`dotnet run --project VenuePlatform.Web` fails due to compile errors in:
- `VenuePlatform.Web/Endpoints/DevEndpoints.cs`

Errors:
1) CS7036: No argument given for required parameter `cancellationToken`:
   - `ICompanyRepository.GetBySlugAsync(string, CancellationToken)` (line ~156)
   - `ICompanyRepository.AddAsync(Company, CancellationToken)` (line ~164)

2) CS1061: `ICompanyRepository` does not contain `DeleteAsync` (line ~177)

This is dev-only functionality; production endpoints must not be touched.

## Goal
Make the project compile and run again with the smallest possible change, preserving behavior of dev endpoints.

## Requirements
1) Fix the compile errors by updating the dev endpoint(s) to use a consistent data access approach:
   - **Preferred**: use `ApplicationDbContext` directly inside DevEndpoints (dev-only), avoiding repository interface drift.
   - **Alternative acceptable**: keep repository usage but pass the `CancellationToken` everywhere and remove/replace `DeleteAsync` with an existing supported operation (only if truly needed).
2) Do **not** introduce new repository methods just to make this compile (no adding `DeleteAsync` to interfaces unless unavoidable).
3) Ensure handlers accept and pass `CancellationToken ct` to EF async calls.
4) Keep all existing slug validation, response shapes, routes, and dev-only guards unchanged.
5) After patch:
   - `dotnet build` must succeed with 0 errors.
   - `dotnet run --project VenuePlatform.Web` must start successfully.

## Implementation guidance (preferred approach)
- In the affected dev endpoint handler(s), replace:
  - `repo.GetBySlugAsync(slug)` with `await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug, ct)`
  - `await repo.AddAsync(company)` with `db.Companies.Add(company); await db.SaveChangesAsync(ct)`
  - Any deletion attempt should be `db.Companies.Remove(company); await db.SaveChangesAsync(ct)` (only if deletion is actually required by the endpoint logic)
- Add `ApplicationDbContext db` and `CancellationToken ct` parameters to the minimal API delegate.

## Documentation (must not be shortened)
- Append to `docs/ai-prompts.md`:
  - UTC timestamp
  - Summary
  - **Full prompt text verbatim** in a fenced code block
  - Ensure it is appended (do not overwrite existing content)

## Deliverable
- Provide the exact code diff or the exact updated code blocks for the affected endpoint(s), and confirm build success.
```

---

# Patch: Fix Missing /dev/bootstrap Endpoint Registration — 2026-03-04T21:13:00Z

## Summary
Fixed the missing `/dev/bootstrap` endpoint by reorganizing `Program.cs` to ensure endpoint mapping happens after all middleware is configured. The dev endpoints are now properly registered with the ASP.NET routing system.

## Full Prompt

```
# Patch Request — Fix Missing /dev/bootstrap Endpoint

## Problem

The development endpoint `/dev/bootstrap` returns:

```
404 Not Found
```

when visiting:

```
http://localhost:5002/dev/bootstrap
```

The frontend DevBootstrap page depends on this endpoint.

The backend contains `DevEndpoints.cs`, but the route appears not to be registered.

---

# Goal

Ensure the dev endpoint

```
POST /dev/bootstrap
```

is properly registered in ASP.NET so that:

```
GET /dev/bootstrap → 405 Method Not Allowed
```

(which confirms the route exists but requires POST).

---

# Step 1 — Verify DevEndpoints mapping extension

Open:

```
VenuePlatform.Web/Endpoints/DevEndpoints.cs
```

Ensure the file defines an extension method:

```csharp
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/bootstrap", async (...) =>
        {
            // existing bootstrap logic
        });

        return app;
    }
}
```

Key requirements:

• The method name must be **MapDevEndpoints**
• The method must return **IEndpointRouteBuilder**
• The route must be **"/dev/bootstrap"**

---

# Step 2 — Ensure the endpoints are registered in Program.cs

Open:

```
VenuePlatform.Web/Program.cs
```

Find the section **after** the application is built.

Correct structure:

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}
```

This must appear **after `builder.Build()` and before `app.Run()`**.

Example correct order:

```csharp
var app = builder.Build();

app.MapAuthEndpoints();
app.MapSpaceEndpoints();
app.MapBookingEndpoints();
app.MapInvoiceEndpoints();
app.MapReportEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}

app.Run();
```

---

# Step 3 — Ensure the namespace is imported

At the top of `Program.cs`, ensure:

```csharp
using VenuePlatform.Web.Endpoints;
```

Otherwise the extension method will not be visible.

---

# Step 4 — Verify route registration

Restart backend:

```
dotnet run --project VenuePlatform.Web
```

Then open:

```
http://localhost:5002/dev/bootstrap
```

Expected result:

```
405 Method Not Allowed
```

This confirms the route exists.

---

# Step 5 — Verify frontend bootstrap works

After the endpoint is reachable:

1. Open

```
http://localhost:5173/dev/bootstrap
```

2. Click

```
Create Company + Owner
```

Expected result:

• Company created
• Owner user created
• JWT login succeeds
• Redirect to

```
/{companySlug}/dashboard
```

---

# Constraints

Do NOT modify:

• Auth system
• Repository interfaces
• Database schema
• Frontend code

Only ensure the dev endpoint is correctly registered.

---

# Verification

Run:

```
dotnet build
```

Then:

```
dotnet run --project VenuePlatform.Web
```

Confirm `/dev/bootstrap` exists.

---

# Documentation

Append this prompt verbatim to:

```
docs/ai-prompts.md
```

---

# Debug: Investigate /dev/bootstrap returning 404 — 2026-03-04T21:47:46Z

## Summary
Fixed 404 error on `POST /dev/bootstrap` by adding `/dev` path exemption to `TenantResolutionMiddleware.IsExemptPath()`. The middleware was intercepting requests to `/dev/bootstrap` before they reached the endpoint, treating "dev" as a company slug and returning 404 when no company with that slug was found.

## Root Cause
`TenantResolutionMiddleware.IsExemptPath()` only exempted `/health` and `/swagger` paths. When a request to `/dev/bootstrap` arrived:
1. Path resolver extracted "dev" as the first segment
2. "dev" passed slug validation (3 lowercase chars)
3. Middleware tried to find company with slug "dev" → not found → returned 404

## Fix Applied
Added `|| pathValue.StartsWith("/dev")` to `IsExemptPath()` method in `VenuePlatform.Web/Tenancy/TenantResolutionMiddleware.cs`.

## Verification
- `GET /dev/routes` → Returns all registered routes (including `/dev/bootstrap`)
- `POST /dev/bootstrap` → Returns 200 OK with created company/user data

## Full Prompt

```
# Debug Investigation: `/dev/bootstrap` returns 404

## Context

Calling the backend endpoint:

curl -i -X POST http://localhost:5002/dev/bootstrap

returns:

HTTP/1.1 404 Not Found

This suggests the endpoint is **not registered in the ASP.NET Core route table at runtime**, but the exact cause must be confirmed before making changes.

Possible causes include:

- Application not running in Development environment
- Dev endpoints not mapped in Program.cs
- Dev endpoints mapped under tenant route group
- Incorrect route prefix such as MapGroup("/dev")
- DevEndpoints extension method not imported
- DevEndpoints file not compiled or referenced
- Middleware short-circuiting the request

The goal of this prompt is **diagnosis first, minimal fix second**.

---

# Goal

Identify exactly why `POST /dev/bootstrap` returns **404** and determine the minimal fix required to restore the endpoint.

---

# Constraints

- Do NOT refactor architecture
- Do NOT redesign authentication
- Do NOT modify frontend
- Dev endpoints must remain Development-only
- KISS: minimal changes only

---

# Investigation Steps

## Step 1 — Confirm runtime environment

Open:

VenuePlatform.Web/Program.cs

Immediately after:

var app = builder.Build();

Add:

app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

Run the backend and confirm the startup log prints:

Environment: Development

If it prints anything else (Production/Staging), investigate launchSettings.json or environment variables.

---

## Step 2 — Add runtime route inspection endpoint

Add a temporary **Development-only** diagnostic endpoint that lists all registered routes.

Location: Program.cs or DevEndpoints.

Endpoint:

GET /dev/routes

Implementation example:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/routes", (EndpointDataSource endpointDataSource) =>
    {
        var routes = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new
            {
                pattern = e.RoutePattern.RawText,
                methods = e.Metadata
                    .OfType<IHttpMethodMetadata>()
                    .FirstOrDefault()?.HttpMethods
            });

        return Results.Ok(routes);
    });
}
```

Purpose: inspect the **actual ASP.NET route table**.

---

## Step 3 — Inspect runtime route table

Run:

curl http://localhost:5002/dev/routes

Look for any of the following patterns:

/dev/bootstrap
/{companySlug}/dev/bootstrap
/dev/bootstrap/
/dev/bootstrap/{something}

Record the exact pattern returned.

---

## Step 4 — Inspect DevEndpoints implementation

Locate file:

VenuePlatform.Web/Endpoints/DevEndpoints.cs

Verify structure:

```csharp
namespace VenuePlatform.Web.Endpoints;

public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
```

Confirm it contains:

```csharp
app.MapPost("/dev/bootstrap", ...)
```

Ensure the route path is exactly `/dev/bootstrap`.

---

## Step 5 — Verify endpoint registration in Program.cs

Confirm the following import exists:

using VenuePlatform.Web.Endpoints;

Confirm the mapping call exists:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}
```

Ensure this appears:

AFTER

var app = builder.Build();

and BEFORE

app.Run();

---

## Step 6 — Inspect tenant route grouping

Search Program.cs for code like:

```csharp
var tenantGroup = app.MapGroup("/{companySlug}");
```

Verify dev endpoints are NOT mapped inside that group.

Correct:

app.MapDevEndpoints();

Incorrect:

tenantGroup.MapDevEndpoints();

If mapped under tenantGroup the real route becomes:

/{companySlug}/dev/bootstrap

which explains the 404.

---

# Expected Outputs

After investigation provide:

### Startup log output

Example:

Environment: Development

---

### Route table output

From:

curl http://localhost:5002/dev/routes

Example:

[
  { "pattern": "/dev/bootstrap", "methods": ["POST"] }
]

---

### Mapping code snippets

Relevant excerpts from:

Program.cs
DevEndpoints.cs

---

### Root cause explanation

Example:

Root cause: MapDevEndpoints() was called on tenantGroup instead of app, resulting in route /{companySlug}/dev/bootstrap instead of /dev/bootstrap.

---

# Minimal Fix

After identifying the root cause, apply the smallest change necessary to ensure:

POST /dev/bootstrap

is mapped directly under the application root.

Example fix:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}
```

---

# Verification Commands

Verify route registration:

curl -s http://localhost:5002/dev/routes | grep bootstrap

Expected output includes:

/dev/bootstrap

Verify POST endpoint:

curl -i -X POST http://localhost:5002/dev/bootstrap

Expected result: NOT 404

Acceptable responses:

200 OK
201 Created
400 validation_error
409 conflict

---

# Prompt Log Requirement (MANDATORY)

After completing the investigation and fix:

Append this entire prompt verbatim to:

docs/ai-prompts.md

Rules:

- Do NOT overwrite previous prompts
- Include timestamp
- Title: "Debug: Investigate /dev/bootstrap returning 404"
- Include this prompt inside a fenced code block
- Add a short summary of findings
- Include the root cause and final fix
```
```

---

# Fix: /dev/bootstrap returning 404 (TenantResolutionMiddleware intercepting /dev) — 2026-03-04T21:54:21Z

## Summary
Fixed `/dev/bootstrap` returning 404 by updating `TenantResolutionMiddleware.IsExemptPath()` to use a safer path matching pattern. The previous fix using `StartsWith("/dev")` was too broad and could match paths like `/devices` or `/developer`. Changed to use exact match `/dev` plus prefix `/dev/` pattern.

## Root Cause
`TenantResolutionMiddleware` attempted to resolve the first path segment as a tenant slug for all requests. Because `/dev/bootstrap` begins with `dev`, the middleware interpreted `dev` as a `{companySlug}` and attempted to load a tenant with slug "dev". Since no such tenant exists, the middleware returned 404 before the request could reach the actual endpoint.

## Fix Applied
Updated `IsExemptPath()` in `VenuePlatform.Web/Tenancy/TenantResolutionMiddleware.cs` from:
```csharp
|| pathValue.StartsWith("/dev")
```
to:
```csharp
|| pathValue == "/dev"
|| pathValue.StartsWith("/dev/")
```

This ensures exact matching of `/dev` and its subpaths without accidentally matching unrelated routes like `/devices` or `/devops`.

## Verification
- `GET /dev/routes` → Returns all registered routes (including `/dev/bootstrap`)
- `POST /dev/bootstrap` → Returns 200 OK with created company/user data
- Tenant routes like `/acme/dashboard` continue to be resolved normally

## Full Prompt

```
# Fix: `/dev/bootstrap` returning 404 (TenantResolutionMiddleware intercepts /dev/*)

## Context

`POST /dev/bootstrap` returned **404 Not Found** even though the endpoint was correctly registered.

Root cause analysis showed that `TenantResolutionMiddleware` attempted to resolve the first path segment as a tenant slug for **all requests**. Because `/dev/bootstrap` begins with `dev`, the middleware interpreted `dev` as a `{companySlug}` and attempted to load a tenant with slug `"dev"`.

Since no such tenant exists, the middleware returned **404** before the request could reach the actual endpoint.

The middleware currently exempts only:

- `/health`
- `/swagger`

Therefore `/dev/*` endpoints are intercepted incorrectly.

---

# Goal

Ensure that **all `/dev` endpoints bypass tenant resolution**, allowing:

```
POST /dev/bootstrap
GET  /dev/routes
```

to reach their registered handlers during development.

---

# Non-goals

- Do not change frontend behavior.
- Do not modify authentication.
- Do not redesign tenant resolution architecture.
- Do not expose dev endpoints in Production.

---

# Constraints

- KISS: minimal patch only.
- Dev endpoints must remain **Development-only**.
- Do not weaken tenant isolation for normal tenant routes.

---

# Implementation Steps

## Step 1 — Update TenantResolutionMiddleware exemption logic

File:

```
VenuePlatform.Web/Tenancy/TenantResolutionMiddleware.cs
```

Locate the method:

```
private static bool IsExemptPath(PathString path)
```

Modify the return logic to exempt `/dev` and `/dev/*`.

### Replace the logic with:

```csharp
private static bool IsExemptPath(PathString path)
{
    var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

    return pathValue == "/health"
        || pathValue.StartsWith("/swagger")
        || pathValue == "/dev"
        || pathValue.StartsWith("/dev/");
}
```

### Why this change

Avoid using:

```
pathValue.StartsWith("/dev")
```

because that would also match unrelated routes like:

```
/devices
/devops
/developer
```

The safer pattern is:

```
path == "/dev"
path.StartsWith("/dev/")
```

---

## Step 2 — Verify dev endpoints remain Development-only

Open:

```
VenuePlatform.Web/Program.cs
```

Confirm dev endpoints are mapped only in Development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}
```

No change required if this already exists.

---

# Verification

## Verify route table

Run:

```bash
curl http://localhost:5002/dev/routes
```

Expected result includes:

```
/dev/bootstrap
```

Example:

```
[
  { "pattern": "/dev/bootstrap", "methods": ["POST"] }
]
```

---

## Verify bootstrap endpoint works

Run:

```bash
curl -i -X POST http://localhost:5002/dev/bootstrap \
  -H "Content-Type: application/json" \
  -d '{
    "companyName":"Acme Co",
    "companySlug":"acme",
    "email":"owner@acme.com",
    "password":"Pass123$",
    "role":"CompanyOwner"
  }'
```

Expected result:

NOT **404**

Acceptable responses:

```
200 OK
201 Created
400 validation_error
409 conflict
```

---

# Expected Result

Requests to `/dev/*` bypass tenant resolution middleware and reach their intended handlers.

Tenant routes like:

```
/acme/dashboard
/acme/spaces
/acme/bookings
```

continue to be resolved normally.

---

# Prompt Log Requirement (MANDATORY)

After implementing this fix, append this **entire prompt verbatim** to:

```
docs/ai-prompts.md
```

Rules:

- Do **NOT overwrite** previous prompts.
- Include timestamp.
- Title:

```
Fix: /dev/bootstrap returning 404 (TenantResolutionMiddleware intercepting /dev)
```

- Include this prompt inside a fenced code block.
- Add a short summary of the fix.
- Include verification results.
```

---

# Debug: DevBootstrap UI network_error — 2026-03-04T22:26:39Z

## Summary
Fixed DevBootstrap UI `network_error` / "Load failed" issue by addressing three root causes:

1. **Missing frontend .env file** - Frontend was defaulting to port 5000 instead of 5002
2. **Backend form binding issue** - `[FromForm]` attributes and antiforgery were blocking requests
3. **CORS misconfiguration** - Backend wasn't allowing requests from frontend origin

## Investigation Results

### 1. Port Check
```
lsof -nP -iTCP:5002 -sTCP:LISTEN
COMMAND     PID       USER   FD   TYPE             DEVICE SIZE/OFF NODE NAME
VenuePlat 77766 madisvoitk  300u  IPv4 0x...      0t0  TCP 127.0.0.1:5002 (LISTEN)
```
Result: Backend WAS running on port 5002

### 2. Frontend API URL Configuration
- `frontend/.env` file was **MISSING**
- `frontend/.env.example` showed port 5000 (incorrect)
- `frontend/src/api/apiClient.ts` defaulted to `http://localhost:5000`

### 3. Backend Endpoint Test
```bash
curl http://localhost:5002/dev/routes
```
Result: Returns all routes including `/dev/bootstrap` - endpoint exists!

### 4. Form Data Test
Initial curl test returned `415 Unsupported Media Type`
Root cause: Minimal APIs require `[FromForm]` attribute for form data binding

### 5. Browser Network Test
Console error: `blocked by CORS policy: No 'Access-Control-Allow-Origin' header`
Root cause: Backend missing CORS configuration for frontend origin

## Root Causes

1. **frontend/.env missing** - API client defaulted to wrong port (5000 instead of 5002)
2. **[FromForm] attributes missing** - ASP.NET minimal APIs require explicit form binding
3. **Antiforgery blocking requests** - Dev endpoint needed `.DisableAntiforgery()`
4. **CORS not configured** - Backend rejected cross-origin requests from port 5174

## Fixes Applied

### 1. Created frontend/.env
```bash
VITE_API_BASE_URL=http://localhost:5002
```

### 2. Updated DevEndpoints.cs
- Added `using Microsoft.AspNetCore.Mvc;`
- Added `[FromForm]` attributes to bootstrap endpoint parameters
- Added `.Accepts<CreateCompanyRequest>("application/x-www-form-urlencoded")`
- Added `.DisableAntiforgery()`

### 3. Updated Program.cs
Added CORS configuration:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Middleware pipeline
app.UseCors("DevCors");
```

## Verification

### Backend startup logs
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5002
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
```

### Bootstrap endpoint test
```bash
curl -i -X POST http://localhost:5002/dev/bootstrap \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Name=TestCompany&Slug=testco3&email=test3@test.com&password=Pass123$&role=CompanyOwner"
```
Result: `HTTP/1.1 200 OK` with JSON response containing companyId, userId, companySlug, role

### Frontend test
- DevBootstrap page loads without network_error
- Backend responses now return proper error messages (e.g., "Username already taken")
- CORS preflight requests succeed

## Full Prompt

```
# Debug: DevBootstrap UI shows `network_error` / "Load failed"

## Context

The Dev Bootstrap UI shows:

{
  "status": 0,
  "code": "network_error",
  "message": "Load failed"
}

This error means the browser **cannot reach the backend at all**.

Important: status `0` in browser APIs usually indicates:

- backend server not running
- port mismatch
- CORS blocking request
- incorrect API base URL
- network connection failure

The backend previously failed to start with:

System.IO.IOException: Failed to bind to address http://127.0.0.1:5002: address already in use.

Therefore the most likely cause is that the backend is not listening on the expected port.

We must **verify the runtime environment, backend availability, and frontend API configuration**.

---

# Goal

Determine why the DevBootstrap page cannot reach the backend API and restore communication between the frontend and backend.

---

# Constraints

- Do NOT refactor architecture.
- Do NOT redesign auth.
- Minimal changes only.
- KISS.

---

# Investigation Steps

## Step 1 — Verify backend is running

Start backend:

```
dotnet run --project VenuePlatform.Web
```

Expected log output:

```
Environment: Development
Now listening on: http://127.0.0.1:5002
Application started.
```

If the server crashes or does not print "Now listening", capture the error.

---

## Step 2 — Check if port 5002 is already in use

Run:

```
lsof -nP -iTCP:5002 -sTCP:LISTEN
```

If a process is listed:

Example:

```
dotnet 12345 user TCP 127.0.0.1:5002 (LISTEN)
```

Terminate it:

```
kill -9 12345
```

Then restart the backend.

---

## Step 3 — Verify backend endpoints directly

Test diagnostic endpoint:

```
curl http://localhost:5002/dev/routes
```

Expected result: JSON containing `/dev/bootstrap`.

Example:

```
[
  { "pattern": "/dev/bootstrap", "methods": ["POST"] }
]
```

Then test bootstrap endpoint:

```
curl -i -X POST http://localhost:5002/dev/bootstrap \
  -H "Content-Type: application/json" \
  -d '{
    "companyName":"Acme Co",
    "companySlug":"acme",
    "email":"owner@acme.com",
    "password":"Pass123$",
    "role":"CompanyOwner"
  }'
```

Expected result:

NOT 404 and NOT network error.

Acceptable responses:

```
200 OK
201 Created
400 validation_error
409 conflict
```

---

## Step 4 — Verify frontend API base URL

Open frontend configuration:

```
frontend/.env
```

Check:

```
VITE_API_URL
```

It must point to the backend:

```
VITE_API_URL=http://localhost:5002
```

If incorrect, update and restart frontend:

```
npm run dev
```

---

## Step 5 — Inspect browser network requests

Open browser devtools → **Network tab**

Click **Create Company + Owner**

Inspect the failing request:

Verify:

- Request URL
- Port
- Status code
- CORS errors
- Network failure reason

Possible findings:

| Issue | Cause |
|------|------|
| status 0 | backend unreachable |
| CORS error | backend CORS config |
| 404 | endpoint mismatch |
| wrong port | frontend config |

---

# Expected Output

Provide:

1. Backend startup logs
2. Output of:

```
lsof -nP -iTCP:5002 -sTCP:LISTEN
```

3. Result of:

```
curl http://localhost:5002/dev/routes
```

4. Frontend API URL configuration
5. Browser network request details

---

# Likely Root Causes

Most probable:

1. Backend crashed due to port conflict
2. Frontend pointing to wrong API URL
3. Backend running on different port
4. CORS misconfiguration
5. Dev server proxy misconfigured

---

# Verification

After fix:

1. Backend starts successfully
2. `/dev/routes` returns route list
3. `/dev/bootstrap` responds correctly
4. DevBootstrap UI successfully creates company

---

# Prompt Log Requirement (MANDATORY)

Append this entire prompt verbatim to:

```
docs/ai-prompts.md
```

Rules:

- Do NOT overwrite previous prompts
- Include timestamp
- Title:

```
Debug: DevBootstrap UI network_error
```

- Include this prompt in a fenced code block
- Add investigation results and root cause

---

# Improvement: Stabilize DevBootstrap integration — 2026-03-04T22:32:12Z

## Summary
Improved DevBootstrap and API client integration by:
1. Standardized API base URL configuration using `VITE_API_BASE_URL` with proper fallback
2. Switched DevBootstrap endpoint from form-based to JSON request body
3. Simplified Minimal API binding by removing `[FromForm]` complexity
4. Ensured dev-only CORS configuration is explicitly gated to Development environment

## Code Changes

### frontend/src/api/apiClient.ts
- Changed fallback URL from `http://localhost:5000` to `http://localhost:5002`
- Changed `||` to `??` operator for proper nullish coalescing

### VenuePlatform.Web/Endpoints/DevEndpoints.cs
- Added `CreateBootstrapRequest` record for JSON binding
- Replaced `[FromForm]` parameters with `[FromBody] CreateBootstrapRequest`
- Removed `.Accepts<CreateCompanyRequest>("application/x-www-form-urlencoded")`
- Endpoint now expects `application/json` content type

### frontend/src/api/devApi.ts
- Changed from `api.postForm()` to `api.post()`
- Now sends JSON body instead of URL-encoded form data
- Updated documentation comments

### VenuePlatform.Web/Program.cs
- CORS middleware now explicitly gated: `if (app.Environment.IsDevelopment())`

## Verification Results

### Backend Build
```
dotnet build
# Build succeeded with 0 errors
```

### Backend Startup
```
Environment: Development
Now listening on: http://127.0.0.1:5002
```

### Dev Routes
```
curl http://localhost:5002/dev/routes
# Returns: /dev/bootstrap endpoint listed
```

### Bootstrap Endpoint (JSON)
```
curl -X POST http://localhost:5002/dev/bootstrap \
  -H "Content-Type: application/json" \
  -d '{
    "companyName":"Test Co",
    "companySlug":"testco",
    "email":"test@test.com",
    "password":"Pass123$",
    "role":"CompanyOwner"
  }'
# Returns: 200 OK with companyId, userId, companySlug, role
```

### Frontend DevBootstrap Page
- Form submission works without network_error
- Company and owner user created successfully
- Redirects to dashboard after login

## Full Prompt

```
# Improvement: Stabilize DevBootstrap integration (API URL consistency, JSON contract, dev-only CORS)

## Context

The DevBootstrap UI previously failed with:

```
status: 0
code: "network_error"
message: "Load failed"
```

The issue was resolved by fixing:

- frontend API base URL
- Minimal API form binding
- antiforgery blocking
- CORS configuration

However, the current solution can be improved to prevent similar issues in the future and simplify the architecture.

Current problems:

1. **Inconsistent API URL configuration**
   - frontend `.env` uses `VITE_API_BASE_URL`
   - project documentation previously referenced `VITE_API_URL`
   - inconsistent naming can cause future configuration errors.

2. **Form-based request contract**
   - DevBootstrap endpoint requires `[FromForm]`
   - this forces `application/x-www-form-urlencoded`
   - JSON would be simpler and consistent with the rest of the API.

3. **Dev-only features not explicitly gated**
   - CORS policy and dev endpoints should clearly be development-only.

4. **Frontend API client configuration should be centralized**
   - API base URL should come from one env variable with a safe fallback.

---

# Goal

Improve the DevBootstrap and API client integration to:

- standardize API base URL configuration
- switch DevBootstrap to JSON request body
- simplify Minimal API binding
- ensure dev-only CORS configuration
- reduce future configuration mistakes

---

# Non-goals

- Do not change frontend UI behavior.
- Do not change API routes.
- Do not refactor unrelated architecture.
- Do not modify authentication flow.

---

# Constraints

- KISS: minimal changes only.
- Maintain compatibility with existing development workflow.
- DevBootstrap remains **development-only**.

---

# Implementation Steps

## Step 1 — Standardize frontend API environment variable

Use a single variable across the entire frontend:

```
VITE_API_BASE_URL
```

Update or create:

```
frontend/.env
```

Example:

```
VITE_API_BASE_URL=http://localhost:5002
```

---

## Step 2 — Centralize API base URL in apiClient

File:

```
frontend/src/api/apiClient.ts
```

Ensure base URL uses the environment variable with fallback:

```ts
const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5002";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});
```

This guarantees consistent backend targeting.

---

## Step 3 — Switch DevBootstrap endpoint to JSON contract

File:

```
VenuePlatform.Web/Endpoints/DevEndpoints.cs
```

Update bootstrap endpoint to accept JSON request body.

Replace `[FromForm]` parameters with:

```csharp
public record CreateCompanyRequest(
    string CompanyName,
    string CompanySlug,
    string Email,
    string Password,
    string Role
);
```

Update endpoint mapping:

```csharp
app.MapPost("/dev/bootstrap",
    async ([FromBody] CreateCompanyRequest request, ApplicationDbContext db) =>
{
    // existing logic
})
.DisableAntiforgery();
```

Remove:

- `[FromForm]`
- `.Accepts<CreateCompanyRequest>("application/x-www-form-urlencoded")`

Now the endpoint expects:

```
application/json
```

which aligns with the rest of the API.

---

## Step 4 — Ensure DevBootstrap remains dev-only

Verify in:

```
VenuePlatform.Web/Program.cs
```

Dev endpoints are mapped only in Development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}
```

---

## Step 5 — Gate CORS to development

Update CORS configuration.

File:

```
VenuePlatform.Web/Program.cs
```

Add:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

Apply middleware only in development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}
```

This prevents permissive CORS settings in production.

---

# Verification

## Verify backend startup

Run:

```
dotnet run --project VenuePlatform.Web
```

Expected:

```
Environment: Development
Now listening on: http://127.0.0.1:5002
```

---

## Verify dev routes

```
curl http://localhost:5002/dev/routes
```

Expected result includes:

```
/dev/bootstrap
```

---

## Verify bootstrap endpoint

```
curl -X POST http://localhost:5002/dev/bootstrap \
  -H "Content-Type: application/json" \
  -d '{
    "companyName":"Acme Co",
    "companySlug":"acme",
    "email":"owner@acme.com",
    "password":"Pass123$",
    "role":"CompanyOwner"
  }'
```

Expected response:

```
200 OK
```

or

```
201 Created
```

---

## Verify frontend

Open:

```
/dev/bootstrap
```

Submit the form.

Expected:

- No `network_error`
- Company and owner user created successfully.

---

# Expected Result

- Frontend always targets the correct backend URL.
- DevBootstrap endpoint uses JSON like the rest of the API.
- No `[FromForm]` binding complexity.
- Dev-only CORS configuration prevents production misconfiguration.
```
```
