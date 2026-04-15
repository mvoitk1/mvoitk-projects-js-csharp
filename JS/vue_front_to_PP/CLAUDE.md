# CLAUDE.md — vue_front_to_PP

## Project overview

Vue 3 + TypeScript frontend for a brand e-commerce app (school project).
Communicates exclusively with the C# backend via its REST API described in Swagger.

## URLs

| Resource | URL |
|---|---|
| Backend API base | `https://mvoitk-cs3.proxy.itcollege.ee/api/v1` |
| Swagger UI | `https://mvoitk-cs3.proxy.itcollege.ee/swagger/index.html` |
| Frontend (prod) | `http://mvoitk-PPfront.proxy.itcollege.ee` → `192.168.181.91:75` |

## Stack

- **Framework:** Vue 3 (Composition API + `<script setup>`)
- **Language:** TypeScript (strict)
- **State:** Pinia
- **Routing:** Vue Router 5
- **Bundler:** Vite 8
- **Linting:** oxlint + ESLint; **Formatting:** Prettier

## Dev commands

```bash
npm run dev          # start dev server
npm run build        # type-check + build
npm run lint         # oxlint + eslint --fix
npm run format       # prettier
```

## Auth model

JWT Bearer token + refresh token.
- On login/register the API returns `{ jwt, refreshToken }`.
- Store both in `localStorage`. Attach `Authorization: Bearer <jwt>` to every authenticated request.
- On 401, attempt `POST /api/v1/Account/RenewRefreshToken` once, then redirect to login.
- Admin routes require the user to hold the `Admin` role (decoded from the JWT).

## Project structure (target)

```
src/
  api/           # thin fetch wrappers per resource (auth, products, cart, orders, admin)
  stores/        # Pinia stores (auth, cart, products, categories, collections, orders)
  router/        # index.ts with route guards
  views/         # one file per page
  components/    # reusable UI pieces
  types/         # TypeScript interfaces mirroring API DTOs
```

## Coding conventions

- Use `<script setup lang="ts">` on every component.
- Define DTO types in `src/types/` and import them; never use `any`.
- API calls live in `src/api/`, not inside components or stores directly.
- Keep components presentational where possible; let stores own data-fetching.
- No CSS framework is prescribed — keep styles scoped unless shared.

## API quick-reference

### Public (no auth)
- `GET /Categories` — category tree
- `GET /Collections` — active collections
- `GET /Products?categoryId=&collectionId=&gender=` — product list
- `GET /Products/{id}` — product detail with variants & images

### Authenticated (user)
- `GET/POST /Cart` and `PUT/DELETE /Cart/items/{id}`
- `GET/POST /Orders` and `GET /Orders/{id}`
- `POST /Account/Logout`

### Admin (`/admin/...`, requires Admin role)
- Full CRUD on categories, collections, products, variants, images
- `PUT /admin/stock/{variantId}`
- `GET/PUT /admin/orders` + status update

## Language / i18n

The app supports **EN and ET**. There is no third-party i18n library — use a lightweight convention:
- `src/stores/locale.ts` holds `locale: 'en' | 'et'` (persisted to `localStorage`).
- A composable `useLocale()` exposes `locale` and a helper `t(en: string, et: string | null | undefined): string` that returns the correct string (falls back to EN if ET is null/empty).
- Admin write DTOs have separate `nameEn`/`nameEt`, `descriptionEn`/`descriptionEt`, etc. — show both inputs in admin forms.
- NavBar shows a language toggle button.

## Auth — role decoding

JWT payload is base64url-encoded JSON. Decode with:
```ts
const payload = JSON.parse(atob(jwt.split('.')[1]))
const isAdmin = payload.role === 'Admin'
```
Store `isAdmin` in the auth store and use it in the router guard for `/admin/*` routes.

## Colors & Sizes

Product variants reference `colorId` and `sizeId` (UUIDs). These are seeded server-side and will be exposed via a lookup endpoint. Fetch them in `src/api/admin.ts` and populate dropdowns in the admin variant form.

## Notes

- `lowestPrice` on the product list item is computed server-side.
- Product images are URL-only (no file upload). Admin form uses a text input for the URL field.
- Pagination is intentionally skipped — all products load in a single request.
