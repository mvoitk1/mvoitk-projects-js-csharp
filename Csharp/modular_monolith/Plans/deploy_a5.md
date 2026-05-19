# Assignment 5 deploy (modular monolith)

## VPS layout

- Host: `192.168.181.91`
- App port: `83` (mapped from container `8080` — see `docker-compose.yml`)
- Postgres host port: `127.0.0.1:5434` (kept loopback-only; bumped from A3's `5433` to avoid collisions)
- Docker compose project name: `csharp_a5`

A3 keeps port `82`. A5 deploys side-by-side on `83`.

## GitLab CI job

The `deploy_csharp_a5` job in `/.gitlab-ci.yml` mirrors `deploy_csharp_a3`:

- Triggers on changes under `Csharp/modular_monolith/**/*` on `main`.
- Reads `CSHARP_A5_*` variables from GitLab CI/CD settings (mirror the `CSHARP_A3_*` set):
  - `CSHARP_A5_POSTGRES_PASSWORD` (required)
  - `CSHARP_A5_JWT_KEY` (required)
  - `CSHARP_A5_APP_PORT` (default `83`)
  - `CSHARP_A5_POSTGRES_DB` (default `csharp_a5`)
  - `CSHARP_A5_POSTGRES_USER` (default `csharp_a5`)
  - `CSHARP_A5_ASPNETCORE_ENVIRONMENT` (default `Production`)
  - `CSHARP_A5_JWT_ISSUER` / `CSHARP_A5_JWT_AUDIENCE` (default `https://shop.local`)
- Writes an `.env` file and runs `docker compose -p csharp_a5 up --build --remove-orphans --detach`.

## Migration run order

The container boot path applies migrations in **Users → Catalog → Sales** order. This is enforced inside the app, not by CI:

- `WebApp/Setup/AppDataInitExtensions.cs` calls `UsersDataInit.MigrateDatabase`, then `catalogContext.Database.Migrate()`, then `salesContext.Database.Migrate()` (in that order) when `DataInitialization:MigrateDatabase` is `true`.
- Users must come first because Catalog/Sales rows reference user IDs (no FK, but the contract assumes the user row exists).
- Each module owns its own EF migrations folder; there is no shared `AppDbContext` anymore.

Seeding follows the same order: `SeedIdentity` (admin user + roles) runs on the Users context; Catalog/Sales currently have no seed step in production.

## Nginx server block

Add the following to the VPS Nginx config (alongside the existing A3 block on port `82`):

```nginx
server {
    listen 80;
    server_name a5.shop.local;   # adjust to your hostname

    location / {
        proxy_pass         http://127.0.0.1:83;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   Upgrade           $http_upgrade;
        proxy_set_header   Connection        "upgrade";
    }
}
```

`WebApp.Setup.AppServicesExtensions.AddForwardedHeaders` is already wired up so `X-Forwarded-*` headers are honored.

## Healthcheck

The container exposes `/health`, which probes all three module `DbContext`s (`UsersDbContext`, `CatalogDbContext`, `SalesDbContext`) via `AddDbContextCheck<T>`. Use this as the readiness probe — it returns `200 Healthy` only when every module's schema is reachable.
