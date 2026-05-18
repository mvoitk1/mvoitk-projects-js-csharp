# CLAUDE.md — Brand E-Commerce Platform (Assignment 3)

## Project overview

Single-brand fashion e-commerce platform built with ASP.NET Core.  
Two user roles: **Customer** (shop) and **Admin** (back-office).  
One solution, one deployed container. REST API + MVC in the same WebApp project.

## Architecture

Clean/Onion layering using the existing solution projects:

```
App.Domain        — entities, value objects, LangStr, Identity
App.DAL.EF        — AppDbContext, repositories, migrations, seeding
App.BLL           — service interfaces + implementations (business logic)
App.DTO           — public DTOs (v1/) used by REST API and BLL
App.Helpers       — shared utilities (JsonHelpers, etc.)
App.Resources     — .resx translation files for Views
Base.Resources    — shared .resx (Common.resx / Common.et.resx)
WebApp            — ASP.NET Core host: MVC controllers, API controllers, Swagger, auth
WebApp.Tests      — xUnit integration tests
```

**Do not** put business logic in controllers. Controllers call BLL services; BLL services call repositories (via AppDbContext directly — no separate repository project needed, use DbContext in BLL).

## Key conventions

### Primary keys
All entities use `Guid` PK inheriting from `BaseEntity` (`Id = Guid.NewGuid()`).

### Translatable fields (LangStr)
Stored as PostgreSQL `jsonb`. Use `[Column(TypeName = "jsonb")]` on LangStr properties.  
Translatable fields: Product (Name, Description, Material), Category (Name), Collection (Name, Description), Color (Name), Size (DisplayName), ProductImage (AltText).

### API versioning
All REST controllers live under `ApiControllers/` and are versioned (`/api/v1/...`).  
Use the existing `AddAppApiVersioning()` / `AddAppSwagger()` setup in Setup/.

### Auth
JWT + refresh token pattern. `AccountController` in `ApiControllers/Identity/` already scaffolded.  
IDOR: every data-access method that touches user-owned data (Cart, Order, Address, Review) must filter by `AppUserId` from the JWT claim — never trust a client-supplied user ID.

### MVC areas
- `Areas/Admin/` — admin back-office (requires `Admin` role)
- Default area — customer-facing shop (anonymous + authenticated)

### DTOs
All API responses use DTOs from `App.DTO/v1/`. Never expose domain entities directly.

### Translations (UI)
Use `App.Resources/Views/` `.resx` files following the existing `Base.Resources` pattern.  
Supported cultures: `en` (default), `et`.  
Localisation middleware is already registered via `AddAppLocalization()`.

### Seeding
Seed data goes in `App.DAL.EF/Seeding/AppDataInit.cs`.  
Seed: Admin role + admin user, one example category, one product with variants.

## Folder layout inside WebApp

```
WebApp/
  ApiControllers/
    Identity/          — AccountController (JWT auth)
    v1/                — REST API controllers (Products, Cart, Orders, ...)
  Areas/
    Admin/
      Controllers/     — MVC admin controllers
      Views/           — Admin razor views
  Controllers/         — Customer MVC controllers (Home, Shop, Cart, Checkout, Account)
  Views/               — Customer razor views
  ViewModels/          — MVC-specific view models (not DTOs)
  Setup/               — Extension methods registered in Program.cs
```

## Workflow rules

- Run EF migrations from solution root:
  ```
  dotnet ef migrations --project App.DAL.EF --startup-project WebApp add <Name>
  dotnet ef database   --project App.DAL.EF --startup-project WebApp update
  ```
- Never scaffold all controllers blindly — only create what the UX requires.
- Do not expose EF navigation collections directly in API responses.
- Keep BLL service methods focused: one method = one use-case.
- All prices are `decimal(18,2)`. Never use `float` or `double` for money.

## Deferred / out of scope for Phase 1

See `DOCUMENTATION.md` section "Deferred Features" for the full list. The main ones:
- Real payment processing (Stripe / bank links)
- Product reviews
- Address book (multiple saved addresses)
- Email notifications
- File upload for product images (URLs only for now)

## CI/CD

CI config is at repo root: `/Csharp/../.gitlab-ci.yml` (one level up from this folder).  
Add a `deploy_csharp_a3` job mirroring the `deploy_csharp_a1` pattern.  
Docker Compose files live in this folder. VPS: `192.168.181.91`, port `82`, Nginx reverse proxy.  
Env vars injected at deploy time via GitLab CI variables (see existing jobs for pattern).
