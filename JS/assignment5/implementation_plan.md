# Implementation Plan — Assignment 5 (React / Next.js)

## 1. Goal

Build a Next.js 16 + React 19 app that talks to the TalTech backend
(`https://taltech.akaver.com/`) using its `ToDo*` entities. The app must:

- Authenticate users with JWT + refresh-token flow (login, register, logout,
  silent refresh on 401).
- Manage cross-cutting state (auth, todos, UI) with React **Context +
  `useReducer`** — no prop drilling.
- Cover the full CRUD surface for the `ToDo*` resources exposed by Swagger
  (categories, priorities, statuses, todo tasks).
- Ship as a standalone Docker container, deployed to my VPS behind the
  existing reverse proxy, with the public URL listed in `README.md`.

Reference: <https://taltech.akaver.com/swagger/index.html>.

## 2. Tech & version assumptions

- Next.js **16.2.6** (App Router). Note: in Next 16, middleware is renamed to
  **Proxy** — use `src/proxy.ts`, not `middleware.ts`.
- React **19.2.4** — use `useActionState`, `useOptimistic`, `use()` where they
  fit.
- TypeScript strict mode (already configured).
- Styling: keep `globals.css` + CSS modules (already wired) — no extra UI
  framework needed for the assignment.
- Validation: `zod` for form schemas + API response parsing at the edges.

No additional runtime deps unless a concrete need appears (`zod`, `jose` only
if we need to inspect JWT expiry client-side — otherwise rely on server
responses).

## 3. Backend surface (verified against Swagger 2026-05-16)

Base URL: `https://taltech.akaver.com`. All paths under `/api/v1/`.

- **Account** (no `identity` segment in the path)
  - `POST /api/v1/Account/Register` → `{ token, refreshToken, firstName, lastName }`
    - body: `{ email, password, firstName, lastName }`
  - `POST /api/v1/Account/Login` → same response shape
    - body: `{ email, password }`
  - `POST /api/v1/Account/RefreshToken` → same response shape
    - body: `{ jwt, refreshToken }` — note: it wants the **old** JWT + refresh token
  - **No `/Logout` endpoint.** Logout is client-side: clear cookies. The refresh
    token cannot be server-side invalidated.
- **ToDo domain** — only three entity sets exist (no `TodoStatuses`):
  - `TodoCategories` — CRUD (`categoryName`, `categorySort`, `tag`)
  - `TodoPriorities` — CRUD (`priorityName`, `prioritySort`)
  - `TodoTasks` — CRUD; task completion is a boolean `isCompleted`, not a status FK
    - fields: `taskName`, `taskSort`, `createdDt`, `dueDt`, `isCompleted`,
      `isArchived`, `todoCategoryId`, `todoPriorityId`, `syncDt`

All ToDo endpoints require `Authorization: Bearer <jwt>`. All entity ids are
GUIDs (string). PUT payloads include `id` and `syncDt` in the body.

## 4. Project layout

```
src/
  app/
    layout.tsx                 # root layout, wraps providers
    page.tsx                   # landing — redirects based on auth
    (auth)/
      login/page.tsx
      register/page.tsx
    (app)/
      layout.tsx               # protected shell (nav, logout)
      todos/page.tsx           # task list + create
      todos/[id]/page.tsx      # task detail/edit
      categories/page.tsx
      priorities/page.tsx
  lib/
    api/
      client.ts                # fetch wrapper (auth header, 401 -> refresh)
      contracts.ts             # TS types mirroring Swagger DTOs
      endpoints.ts             # typed endpoint functions
    auth/
      tokens.ts                # cookie read/write helpers (httpOnly)
      session.ts               # server-side getSession()
  state/
    auth/
      AuthContext.tsx          # provider + hook
      authReducer.ts
      authActions.ts           # async thunks-ish helpers
    todos/
      TodosContext.tsx
      todosReducer.ts
  components/
    forms/LoginForm.tsx
    forms/RegisterForm.tsx
    todos/TodoList.tsx
    todos/TodoForm.tsx
    layout/NavBar.tsx
  proxy.ts                     # Next 16 Proxy (route guard / optimistic check)
```

Route groups `(auth)` and `(app)` keep public vs. protected layouts separate
without affecting URLs.

## 5. Auth flow

### 5.1 Token storage

- **Access token + refresh token are stored in httpOnly, Secure, SameSite=Lax
  cookies**, set from a server action / route handler. Never expose tokens to
  client JS — eliminates XSS exfiltration.
- Cookie names: `at` (access), `rt` (refresh). `at` lifetime = backend's JWT
  TTL; `rt` lifetime = backend's refresh TTL.

### 5.2 Login (`src/app/(auth)/login/page.tsx`)

- Client component using `useActionState` against a server action
  `loginAction(state, formData)` in `src/app/actions/auth.ts`.
- Server action:
  1. `zod`-validates input.
  2. Calls `POST /Account/Login` on the backend.
  3. On success, writes `at` + `rt` cookies via `cookies()` from
     `next/headers`.
  4. `redirect('/todos')`.
- Same shape for `registerAction` (calls `Register`, then `Login`, or returns
  to login screen depending on backend behavior).

### 5.3 Authenticated requests

`src/lib/api/client.ts` exposes `apiFetch(path, init)`:

- Always called from **server components / server actions / route handlers**
  (so cookies are accessible and tokens stay server-side).
- Reads `at` from `cookies()`, sets `Authorization` header.
- On `401`:
  1. Calls refresh endpoint with `rt`.
  2. On success: rewrites `at`/`rt` cookies, retries original request once.
  3. On failure: clears cookies, throws a typed `Unauthorized` error → caller
     redirects to `/login`.
- The retry-once guarantee prevents loops.

### 5.4 Route protection

Two layers:

1. **`src/proxy.ts`** (Next 16's renamed middleware) — cheap optimistic check:
   if `at` cookie missing and request targets `/todos`, `/categories`, etc.,
   redirect to `/login`. Per the Next 16 docs, Proxy is for optimistic checks
   only, not full session validation.
2. **Server-side `getSession()`** in protected layouts — does the real check
   by calling a `/me`-style endpoint (or decoding the JWT's `exp` only as a
   hint, never as the sole auth signal). If invalid, redirect.

### 5.5 Logout

Server action: clears both cookies, then `redirect('/login')`. The backend has
no `/Logout` endpoint, so the refresh token stays valid server-side until it
expires naturally — acceptable since the client no longer holds it.

## 6. State management (Context + Reducer)

Two contexts, both client-side, kept thin:

### 6.1 `AuthContext`

- State shape: `{ status: 'anon' | 'authed', user: { id, email, name } | null }`.
- The reducer only mirrors what the server already decided — the source of
  truth is the cookies. The context exists so client components (NavBar,
  conditional UI) can read auth state without prop-drilling and so we can
  trigger `logoutAction` from anywhere.
- Hydrated by passing `initialUser` from the root server layout into the
  provider.

### 6.2 `TodosContext`

- State: `{ items: TodoTask[], filter, sort, optimisticUpdates }`.
- Actions: `SET`, `ADD`, `UPDATE`, `DELETE`, `SET_FILTER`, `SET_SORT`.
- The list page (server component) fetches initial data and hydrates the
  provider; subsequent mutations go through server actions and dispatch to
  the reducer for instant UI.
- `useOptimistic` layered on top of the reducer for create/edit/delete.

Both providers expose a custom hook (`useAuth`, `useTodos`) that throws if
used outside the provider — this is the "no prop drilling" enforcement.

## 7. Pages and UX

- `/login`, `/register` — forms with `useActionState`, inline field errors
  from zod.
- `/todos` — list with filter (by status/priority/category), inline create
  form, optimistic add/toggle/delete.
- `/todos/[id]` — edit form, delete button.
- `/categories`, `/priorities` — simple CRUD tables. These are required
  because they back the dropdowns on the task form.
- Navigation: persistent NavBar (client component) reading `AuthContext` for
  the user email + logout button.

Empty/loading states via `loading.tsx` + `error.tsx` in each route segment.

## 8. Error handling

- `apiFetch` returns a discriminated union `{ ok: true, data } | { ok: false,
  error: ApiError }` — no thrown exceptions at the data layer (except the
  `Unauthorized` redirect signal).
- `error.tsx` boundaries catch unexpected throws in each route group.
- Form errors are surfaced via the `useActionState` state object.

## 9. Docker & deployment

### 9.1 Dockerfile (multi-stage)

```
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public
EXPOSE 3000
CMD ["node", "server.js"]
```

Requires `output: 'standalone'` in `next.config.ts`.

### 9.2 Env vars

- `BACKEND_BASE_URL=https://taltech.akaver.com` (server-only).
- `NEXT_PUBLIC_APP_URL=https://<my-vps-host>/assignment5` (only if needed for
  absolute URLs).
- Cookie secret / signing not strictly required since tokens come from the
  backend; cookies are httpOnly + Secure in prod regardless.

### 9.3 VPS deployment

- Build image, push (or build on VPS via `docker compose`).
- Slot into existing reverse proxy (Caddy/Nginx) on the VPS, on a sub-path or
  sub-domain.
- Public URL recorded in `README.md` per assignment requirement.

## 10. Build / test / smoke checklist

Before pushing the final commit:

- [ ] `npm run build` succeeds, no type errors, no `any` leaking.
- [ ] `npm run lint` clean.
- [ ] Login → todos list → create → edit → delete → logout flow works
      end-to-end against the live backend.
- [ ] Forcing `at` cookie deletion triggers silent refresh, not a redirect.
- [ ] Forcing both cookies to expire bounces to `/login`.
- [ ] Hitting `/todos` while unauthenticated redirects to `/login`.
- [ ] Docker image runs locally on port 3000 and behaves the same.
- [ ] Public URL added to `README.md`.

## 11. Order of work

1. Wire `src/lib/api/contracts.ts` + `endpoints.ts` from Swagger.
2. Build `apiFetch` with refresh logic + unit-test the refresh path manually.
3. Auth server actions (`login`, `register`, `logout`) + cookie helpers.
4. `proxy.ts` + protected layout with `getSession()`.
5. `AuthContext` + NavBar.
6. Todos list page (read path) end-to-end.
7. Todos mutations (create/update/delete) with `useOptimistic`.
8. Categories / priorities CRUD.
9. `loading.tsx` / `error.tsx` polish.
10. Dockerfile + standalone output + deploy.
11. README with public URL + run instructions.

## 12. Open questions — resolved 2026-05-16

- ✅ Refresh endpoint: `POST /api/v1/Account/RefreshToken`, body
  `{ jwt, refreshToken }`. Optional query `expiresInSeconds`.
- ✅ Backend does **not** return `expiresIn`. We refresh **reactively on 401**
  only. (Optionally could decode JWT `exp` later for preemptive refresh.)
- ✅ `Register` returns the same `{ token, refreshToken, firstName, lastName }`
  shape as `Login` — so registration auto-logs in.
