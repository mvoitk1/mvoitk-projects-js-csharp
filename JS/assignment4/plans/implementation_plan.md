# Vue 3 ToDo App — Implementation Plan

**API base**: https://taltech.akaver.com/ (this is the backend deployed from this same repo — the ASP.NET Core WebApp project)  
**Swagger**: https://taltech.akaver.com/swagger/index.html  
**Stack**: Vue 3, Vite, Pinia, Vue Router, Axios, TypeScript

> **Note**: The Vue app lives in `vue-app/` at the repo root. It targets the backend in this same repo deployed at taltech.akaver.com. The correct API versioning prefix is `/api/v1.0/` (not `/api/v1/`). ToDo entities are called **TodoTasks** (`/api/v1.0/TodoTasks`), not TodoItems.

---

## Phase 1 — Project Scaffold

- [ ] Create Vue 3 + TypeScript project with Vite inside `vue-app/`
- [ ] Install dependencies: `pinia`, `vue-router`, `axios`
- [ ] Set up folder structure:
  ```
  vue-app/
    src/
      api/          # Axios instance + typed API calls
      stores/       # Pinia stores (auth, todo, todoCategory)
      router/       # Vue Router with guards
      views/        # Page components
      components/   # Reusable UI components
      types/        # TypeScript interfaces matching API DTOs
  ```

---

## Phase 2 — API Layer

- [ ] Create `src/api/axios.ts` — Axios instance with:
  - `baseURL` pointing to taltech.akaver.com
  - Request interceptor to attach JWT `Bearer` token from store
  - Response interceptor to handle 401 → trigger silent token refresh, then retry
- [ ] Create `src/api/auth.ts` — typed wrappers for:
  - `POST /api/v1.0/Account/Register`
  - `POST /api/v1.0/Account/Login`
  - `POST /api/v1.0/Account/RefreshToken`
- [ ] Create `src/api/todoTasks.ts` — typed wrappers for TodoTasks CRUD endpoints (`/api/v1.0/TodoTasks`)
- [ ] Create `src/api/todoCategory.ts` — typed wrappers for TodoCategory endpoints (`/api/v1.0/TodoCategories`)
- [ ] Define `src/types/` interfaces from Swagger schema (JWTResponse, TodoItem, TodoCategory, etc.)

---

## Phase 3 — Auth Store (Pinia)

- [ ] `src/stores/auth.ts`:
  - State: `jwt`, `refreshToken`, `user` (email/firstName)
  - Actions: `login()`, `register()`, `logout()`, `refreshTokens()`
  - Getters: `isAuthenticated`, `currentUser`
  - Persist `jwt` + `refreshToken` to `localStorage` so auth survives page reload
  - On app init, attempt silent refresh if `refreshToken` exists

---

## Phase 4 — Vue Router + Guards

- [ ] Define routes:
  - `/login` — public
  - `/register` — public
  - `/` (redirect to `/todos`) — protected
  - `/todos` — protected, list view
  - `/todos/:id` — protected, detail/edit view
  - `/categories` — protected
- [ ] Navigation guard (`router.beforeEach`): redirect unauthenticated users to `/login`
- [ ] Redirect already-authenticated users away from `/login`/`/register`

---

## Phase 5 — Todo Store (Pinia)

- [ ] `src/stores/todoTasks.ts`:
  - State: `items: TodoTask[]`, `loading`, `error`
  - Actions: `fetchAll()`, `create()`, `update()`, `remove()`
- [ ] `src/stores/todoCategory.ts`:
  - Same pattern for categories

---

## Phase 6 — Views & Components

- [ ] `LoginView.vue` — form with email/password, calls auth store `login()`
- [ ] `RegisterView.vue` — form for registration
- [ ] `TodoListView.vue` — list all todos, add new, delete
- [ ] `TodoDetailView.vue` — edit a single todo
- [ ] `CategoryListView.vue` — manage categories
- [ ] `NavBar.vue` — shows user info, logout button; conditionally rendered

---

## Phase 7 — JWT Refresh Token Flow (Critical)

Silent refresh strategy:
1. Store `jwt` and `refreshToken` in Pinia + `localStorage`
2. Axios response interceptor catches 401
3. Calls `refreshtoken` endpoint with current refresh token
4. Stores new `jwt` + `refreshToken`
5. Retries the failed request with new token
6. If refresh also fails → logout and redirect to `/login`
7. Queue concurrent requests during refresh to avoid race conditions

---

## Phase 8 — Docker & Deployment

- [ ] Create `vue-app/Dockerfile`:
  - Multi-stage: `node:22-alpine` build stage → `nginx:alpine` serve stage
  - Build: `npm run build` → copy `dist/` to nginx html
  - Include `nginx.conf` with `try_files` for SPA routing
- [ ] Add `vue-app` service to `docker-compose.production.yml`
- [ ] Configure environment variable `VITE_API_BASE_URL` so the base URL is not hardcoded
- [ ] Deploy to VPS, expose on a public port or subdomain
- [ ] Update `README.md` with the public URL

---

## Phase 9 — Polish ⚠️ DEFERRED — do not implement unless explicitly requested

- [ ] Loading spinners on async operations
- [ ] Error messages shown to user on API failures
- [ ] Form validation (client-side)
- [ ] Auto-redirect to intended page after login (stored route)

---

## Key API Endpoints (from Swagger)

| Action | Method | Path |
|---|---|---|
| Register | POST | `/api/v1.0/Account/Register` |
| Login | POST | `/api/v1.0/Account/Login` |
| Refresh token | POST | `/api/v1.0/Account/RefreshToken` |
| Get todo tasks | GET | `/api/v1.0/TodoTasks` |
| Create todo task | POST | `/api/v1.0/TodoTasks` |
| Update todo task | PUT | `/api/v1.0/TodoTasks/{id}` |
| Delete todo task | DELETE | `/api/v1.0/TodoTasks/{id}` |
| Get categories | GET | `/api/v1.0/TodoCategories` |
| Create category | POST | `/api/v1.0/TodoCategories` |

---

## Decisions & Notes

- TypeScript throughout for type safety matching API contracts
- Pinia over Vuex — simpler, Vue 3 native
- Axios over fetch — interceptors make the refresh token flow clean
- Nginx serves the built SPA, handles SPA routing via `try_files $uri /index.html`
- Environment variable `VITE_API_BASE_URL` set at Docker build time
