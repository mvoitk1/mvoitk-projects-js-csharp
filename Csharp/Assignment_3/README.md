# Assignment 3

ASP.NET Core e-commerce application with:

- Razor MVC storefront
- Admin dashboard for catalogue and orders
- REST API with JWT authentication
- PostgreSQL + Entity Framework Core
- English and Estonian localization

## Live App

Deployed version: https://mvoitk-cs3.proxy.itcollege.ee/

## Tech Stack

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- JWT authentication
- Swagger / OpenAPI
- Docker Compose
- xUnit

## Project Structure

- `WebApp` - web host, MVC views, API controllers, setup/configuration
- `App.BLL` - business logic services
- `App.DAL.EF` - EF Core context, migrations, seed data
- `App.Domain` - domain entities and enums
- `App.DTO` - API DTOs
- `App.Helpers` - helper utilities
- `App.Resources` and `Base.Resources` - localization resources
- `WebApp.Tests` - unit and integration tests

## Main Features

- Browse products, collections, and categories
- Filter shop items by category, collection, and gender
- Product detail pages with variants and images
- Shopping cart and checkout flow
- Order history for signed-in users
- Admin area for products, categories, collections, stock, and orders
- Versioned API under `/api/v1`
- Swagger UI under `/swagger`

## Run Locally

### Prerequisites

- .NET SDK 10
- PostgreSQL

### Local development

1. Start PostgreSQL.
2. Make sure the connection string in `WebApp/appsettings.json` matches your local database.
3. Run the app:

```bash
dotnet run --project WebApp
```

Local URL:

- `http://localhost:5256`
- `https://localhost:7147`

## Run With Docker

Create a `.env` file for Docker Compose with at least:

```env
POSTGRES_DB=webapp2526s
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
JWT_KEY=replace-with-a-long-random-secret
JWT_ISSUER=https://shop.local
JWT_AUDIENCE=https://shop.local
APP_PORT=82
ASPNETCORE_ENVIRONMENT=Production
```

Then start the stack:

```bash
docker compose up --build
```

The app is exposed on `http://localhost:82` by default.

## Database

The application is configured to:

- run EF Core migrations on startup
- seed identity data on startup
- seed sample catalogue data on startup

Default seeded admin user:

- Email: `admin@shop.ee`
- Password: `Admin.Password.1`

## Testing

Run all tests:

```bash
dotnet test
```

## API

Base path:

```text
/api/v1
```

Included areas:

- `account`
- `products`
- `categories`
- `collections`
- `cart`
- `orders`
- `admin/*`

For interactive API docs, open `/swagger`.

## Notes

- The project includes both MVC pages and JSON API endpoints.
- Supported UI cultures are English (`en`) and Estonian (`et`).
- Additional technical and planning notes are available in `README-TECH.md` and `DOCUMENTATION.md`.


admin two factor auth codes:
75KDJ-WNHCG 3NP6W-HXN5F
NP2CD-WW844 RRQQB-X3KYM
5K7WX-V7JMF GJ28F-3HQ45
47T8W-J4GPM 7WRTP-8Q5HG
67P95-QCB6Y 3JDH5-PMX7N