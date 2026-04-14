# Assignment 4.1 — Documentation

---

## Phase 1 — Types

**What:** Created `src/types/index.ts` defining all TypeScript interfaces: `JWTResponse`, `LoginRequest`, `RegisterRequest`, `RefreshTokenRequest`, `TodoTask`, `TodoTaskCreate`, `TodoCategory`, `TodoCategoryCreate`, `TodoPriority`, `TodoPriorityCreate`.

**Why:** Centralising all shared types in one file keeps the API layer, stores, and views consistent. TypeScript catches shape mismatches at compile time rather than at runtime against the live backend.

**How:** Plain `export interface` declarations, no runtime code. Imported with `import type` throughout the codebase to ensure no accidental runtime cost.

---

## Phase 2 — API Layer

**What:** Created four files under `src/api/`:
- `axios.ts` — Axios instance with proactive JWT expiry check (request interceptor) and 401/403 handling (response interceptor).
- `auth.ts` — login, register, refreshToken wrappers.
- `todoTasks.ts`, `todoCategory.ts`, `todoPriority.ts` — typed CRUD wrappers (`getAll`, `getById`, `create`, `update`, `remove`).

**Why:** Centralising HTTP logic in one place means every request automatically gets the auth header and token refresh without each view having to think about it. Separating the axios instance from the route-specific wrappers keeps each file small and testable.

**How:** The request interceptor decodes the JWT payload (base64), compares `exp - 30` against `Date.now() / 1000`, and calls `POST /api/v1.0/Account/RefreshToken` using a plain `axios.post` (not the instance) to avoid an interceptor loop. The response interceptor distinguishes 403 (redirect to `/dashboard?error=forbidden`, no retry) from 401 (queue concurrent requests, attempt one refresh, drain the queue, redirect to `/login` on failure).

---

## Phase 3 — Auth Store

**What:** Created `src/stores/auth.ts` (Pinia Composition API style) with reactive state for `jwt`, `refreshToken`, `firstName`, `lastName`, initialised from `localStorage`.

**Why:** Pinia gives reactive, component-accessible state with no boilerplate. Initialising from `localStorage` on store creation means the auth state survives hard page reloads without an extra server round-trip.

**How:** `_persist()` writes to both the reactive refs and `localStorage`, then calls `scheduleRefresh()`. `scheduleRefresh()` decodes `exp` from the JWT, clears any existing timer, and sets a `setTimeout` to call `refreshTokens()` 60 seconds before expiry. `logout()` clears the timer, refs, localStorage, and manually resets all three data stores (Composition API stores have no `$reset()`).

---

## Phase 4 — Data Stores

**What:** Three Pinia stores (`src/stores/todoTasks.ts`, `todoCategory.ts`, `todoPriority.ts`) each with `items`, `loading`, `error` state and `fetchAll`, `create`, `update`, `remove` actions.

**Why:** Storing fetched data in Pinia means views share the same list — navigating away and back does not re-fetch unless explicitly needed, and any view can mutate the list optimistically.

**How:** Every action follows the same pattern: set `loading = true`, clear `error`, call the API wrapper, update `items` directly (push/splice/filter), catch errors into `error`, and always clear `loading` in `finally`. `update` uses `findIndex + splice` to swap a single item in-place, preserving reactivity.

---

## Phase 5 — Router + Navigation Guards

**What:** Rewrote `src/router/index.ts` with all eight routes (including a redirect from `/` to `/dashboard`). All routes except `/login` and `/register` are protected (no `meta: { public: true }`). Added an async `beforeEach` guard.

**Why:** The guard needs to be async so it can `await auth.refreshTokens()` before deciding to redirect. This enables silent session recovery on hard page reload — the user stays on the protected route if the refresh succeeds.

**How:** The guard checks `isAuthenticated`; if false and a `refreshToken` exists in `localStorage`, it attempts a token refresh. If successful it returns `undefined` (allow navigation). If failed or no refresh token, it returns `'/login'`. Public routes redirect authenticated users to `/dashboard` to prevent back-navigation to the login page.

---

## Phase 6 — Views and Components

**What:** Implemented all views and the NavBar component:
- `App.vue` — mounts NavBar conditionally on `auth.isAuthenticated`
- `NavBar.vue` — links to Dashboard, Categories, Priorities; shows the user's name; Logout button
- `LoginView.vue`, `RegisterView.vue` — forms with inline error display
- `DashboardView.vue` — task list with inline complete-toggle, delete, and link to detail; shows 403 warning from query string
- `CreateTaskView.vue` — create form with category/priority dropdowns
- `TodoDetailView.vue` — edit form loaded by task ID, Save and Delete actions
- `CategoryListView.vue`, `PriorityListView.vue` — list + inline create form + delete

**Why:** Each view uses only `{{ }}` text interpolation — never `v-html` — to eliminate XSS risk from server-supplied strings. Dropdowns are populated from the shared Pinia stores so categories/priorities are fetched at most once per session.

**How:** All views use `<script setup lang="ts">` (Composition API). `onMounted` triggers store fetches. The dashboard 403 warning reads `route.query.error` — this is set by the Axios response interceptor when it catches a 403 and redirects.

---

## Phase 7 — Docker and nginx

**What:** Created `Dockerfile` (multi-stage Node 22 build → nginx:alpine), `nginx.conf` with security headers, and `docker-compose.yml` mapping host port `76` to container port `80` on the `infra` external network.

**Why:** The multi-stage build keeps the final image small (no Node runtime, only static files). nginx serves the SPA with `try_files ... /index.html` so Vue Router history mode works on hard reload. Security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy) compensate for tokens being stored in localStorage.

**How:** `VITE_API_BASE_URL` is injected as a Docker build arg so the same Dockerfile works for local dev and production without code changes. `style-src 'unsafe-inline'` is required because Vue 3 injects scoped component styles at runtime.

---

## Phase 8 — CI/CD

**What:** Added `deploy_javascript_a4_1` job to `.gitlab-ci.yml` at the repo root.

**Why:** The existing CI pipeline deploys all other assignments; following the same pattern keeps the deployment process consistent and avoids manual steps.

**How:** The job runs on the `main` branch with the `shared` runner tag. It creates the `infra` Docker network (ignoring error if it already exists), changes into `JS/assignment4.1`, and runs `docker compose up --build --remove-orphans --detach`. The app is served at `https://mvoitk-vue2.proxy.itcollege.ee` (VPS port `76`).
