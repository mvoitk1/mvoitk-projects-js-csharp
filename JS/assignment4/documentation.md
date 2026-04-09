# Project Documentation

Every implemented feature or module must be documented here. Each entry must answer three questions: **what** was built, **why** it was built that way, and **how** it works.

---

## Phase 1 — Project Scaffold

**What**: Vue 3 + TypeScript SPA scaffolded with Vite inside `vue-app/`. Dependencies installed: `pinia`, `vue-router`, `axios`.

**Why**: Vite provides fast HMR and first-class TypeScript support. Vue 3 Composition API gives better type inference and tree-shaking.

**How**: `npm create vite@latest . -- --template vue-ts` bootstrapped the project. Folder structure follows separation of concerns: `api/`, `stores/`, `router/`, `views/`, `components/`, `types/`.

---

## Phase 2 — API Layer

**What**: `src/api/axios.ts` — Axios instance with JWT request interceptor and 401 silent-refresh response interceptor. `src/api/auth.ts`, `todoTasks.ts`, `todoCategory.ts` — typed wrappers for all REST endpoints. `src/types/index.ts` — TypeScript interfaces matching API DTOs.

**Why**: Centralising the Axios instance means all JWT attachment and token refresh logic lives in one place. Typed wrappers prevent passing wrong shapes to API calls.

**How**: The request interceptor reads `jwt` from `localStorage` and attaches `Authorization: Bearer <token>`. The response interceptor catches 401s, queues concurrent requests while one refresh is in flight (using a `failedQueue` array), calls `POST /api/v1.0/Account/RefreshToken`, stores new tokens, then retries all queued requests. On refresh failure, tokens are cleared and the user is redirected to `/login`.

---

## Phase 3 — Auth Store (Pinia)

**What**: `src/stores/auth.ts` — Pinia store managing JWT, refresh token, and user name. Actions: `login`, `register`, `logout`, `refreshTokens`.

**Why**: Pinia is the Vue 3 native state library — simpler than Vuex, fully typed. Persisting tokens to `localStorage` means auth survives page reload without a round-trip to the server.

**How**: On store init, `jwt`/`refreshToken`/`firstName`/`lastName` are read from `localStorage`. `_persist()` writes all four to both reactive refs and storage atomically. `main.ts` calls `refreshTokens()` on startup if a refresh token exists but the JWT is absent.

---

## Phase 4 — Vue Router + Guards

**What**: `src/router/index.ts` — routes for `/login`, `/register`, `/todos`, `/todos/:id`, `/categories`. `beforeEach` guard enforces authentication.

**Why**: Declarative route guards keep auth enforcement out of individual components.

**How**: Routes not tagged `meta: { public: true }` redirect to `/login` if `auth.isAuthenticated` is false. Authenticated users hitting `/login` or `/register` are redirected to `/todos` to prevent redundant auth pages.

---

## Phase 5 — Todo & Category Stores (Pinia)

**What**: `src/stores/todoTasks.ts` and `src/stores/todoCategory.ts` — state (`items`, `loading`, `error`) and CRUD actions (`fetchAll`, `create`, `update`, `remove`).

**Why**: Keeping list state in a store means the TodoListView and TodoDetailView share the same data without prop drilling or redundant API calls.

**How**: Each mutating action sets `loading = true`, calls the API wrapper, updates `items` optimistically/reactively, and sets `error` on failure. Errors are also re-thrown so views can react if needed.

---

## Phase 6 — Views & Components

**What**: `LoginView.vue`, `RegisterView.vue`, `TodoListView.vue`, `TodoDetailView.vue`, `CategoryListView.vue`, `NavBar.vue`.

**Why**: Each view maps to a route. NavBar is a separate component so it renders once in `App.vue` and is conditionally hidden on auth pages via `v-if="auth.isAuthenticated"`.

**How**: Auth views call store `login()`/`register()` and push to `/todos` on success. `TodoListView` fetches on `onMounted`, supports inline create (name only) and delete, and toggles completion in-place. `TodoDetailView` loads the task by id, fetches categories for the select, and PUTs the full object on save. `CategoryListView` mirrors the todo list pattern for categories.

---

## Phase 7 — JWT Refresh Token Flow

**What**: Silent token refresh implemented in `src/api/axios.ts` response interceptor.

**Why**: JWTs expire. Without silent refresh, users would be logged out mid-session. This flow is transparent to all callers.

**How**: On 401, the interceptor checks for a refresh token in `localStorage`. If `isRefreshing` is already `true` (concurrent request), it queues a promise in `failedQueue`. Otherwise it sets `isRefreshing = true`, calls `POST /api/v1.0/Account/RefreshToken`, saves new tokens, drains the queue with the new token, then retries the original request. If the refresh call itself fails, it clears all tokens and redirects to `/login`.

---

## Phase 8 — Docker & Deployment

**What**: `vue-app/Dockerfile` (multi-stage build), `vue-app/nginx.conf` (SPA routing), `vue-app` service added to `docker-compose.production.yml`.

**Why**: Multi-stage keeps the final image small — only the nginx alpine image ships, not Node. `try_files $uri $uri/ /index.html` is required so Vue Router's history mode works on hard refresh.

**How**: Stage 1 (`node:22-alpine`) runs `npm ci && npm run build` with `VITE_API_BASE_URL` injected as a build arg. Stage 2 (`nginx:alpine`) copies `dist/` into the nginx html root and uses a custom `nginx.conf`. The service is exposed on host port `77`, mapped to container port `80`, and joins the `infra` Docker network alongside the ASP.NET backend.
