# CLAUDE.md — Assignment 4.1 Vue Frontend

Everything a new AI agent needs to work on this project without asking the user to repeat context.

---

## What this project is

A Vue 3 SPA frontend that authenticates users and manages **ToDo** entities (tasks, categories, priorities) against the shared TalTech backend REST API at `https://taltech.akaver.com/`. This is a university assignment focused on proper JWT + refresh token security, Pinia state management, and Vue Router navigation guards.

**Swagger**: `https://taltech.akaver.com/swagger/index.html`  
**Backend source**: `https://git2.akaver.com/taltech-public/com.akaver.taltech`

This app does **not** own the backend. All auth tokens come back as JSON response bodies — `httpOnly` cookie storage is not available.

---

## Repository context

- Repo root: `mvoitk-projects_js_csharp/`
- This project lives at: `JS/assignment4.1/`
- A previous implementation lives at: `JS/assignment4/vue-app/` — read it for reference, but do not copy/paste blindly; this is a fresh scaffold
- CI/CD: `.gitlab-ci.yml` at repo root handles Docker deployments; a new job for `assignment4.1` may need to be added
- Deployment target: VPS at `mvoitk-vue2.proxy.itcollege.ee` (IP `192.168.181.91:76`)

---

## Tech stack (already installed)

| Package | Version | Purpose |
|---|---|---|
| `vue` | ^3.5 | UI framework — Composition API only |
| `vue-router` | ^5.0 | Client-side routing + navigation guards |
| `pinia` | ^3.0 | State management |
| TypeScript | ~6.0 | Type safety throughout |
| Vite | ^8.0 | Dev server and build tool |

**axios is NOT yet installed** — it must be added (`npm install axios`).

---

## Project structure to build

```
src/
  api/
    axios.ts          # Axios instance, JWT interceptors, 401/403 handling
    auth.ts           # Login, register, refresh token API wrappers
    todoTasks.ts      # CRUD for /api/v1.0/TodoTasks
    todoCategory.ts   # CRUD for /api/v1.0/TodoCategories
    todoPriority.ts   # CRUD for /api/v1.0/TodoPriorities
  stores/
    auth.ts           # JWT state, persist to localStorage, scheduleRefresh
    todoTasks.ts      # items/loading/error + fetchAll/create/update/remove
    todoCategory.ts   # same pattern as todoTasks
    todoPriority.ts   # same pattern as todoTasks
  router/
    index.ts          # Routes + async beforeEach guard
  views/
    LoginView.vue
    RegisterView.vue
    DashboardView.vue # main todo list
    TodoDetailView.vue
    CreateTaskView.vue
    CategoryListView.vue
    PriorityListView.vue
  components/
    NavBar.vue        # shown only when isAuthenticated
  types/
    index.ts          # All TypeScript interfaces
  main.ts
  App.vue
```

---

## Backend API — key endpoints

Base URL: `https://taltech.akaver.com` (or `VITE_API_BASE_URL` env var)

### Auth

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/v1.0/Account/Login` | `{ email, password }` | `JWTResponse` |
| POST | `/api/v1.0/Account/Register` | `{ email, password, firstName, lastName }` | `JWTResponse` |
| POST | `/api/v1.0/Account/RefreshToken` | `{ jwt, refreshToken }` | `JWTResponse` |

`JWTResponse`: `{ token: string, refreshToken: string, firstName: string, lastName: string }`

### ToDo Tasks

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1.0/TodoTasks` | Returns user's tasks |
| GET | `/api/v1.0/TodoTasks/{id}` | Single task |
| POST | `/api/v1.0/TodoTasks` | Create task |
| PUT | `/api/v1.0/TodoTasks/{id}` | Update task (full object required) |
| DELETE | `/api/v1.0/TodoTasks/{id}` | Delete task |

### ToDo Categories

| Method | Path |
|---|---|
| GET | `/api/v1.0/TodoCategories` |
| POST | `/api/v1.0/TodoCategories` |
| PUT | `/api/v1.0/TodoCategories/{id}` |
| DELETE | `/api/v1.0/TodoCategories/{id}` |

### ToDo Priorities

| Method | Path |
|---|---|
| GET | `/api/v1.0/TodoPriorities` |
| POST | `/api/v1.0/TodoPriorities` |
| PUT | `/api/v1.0/TodoPriorities/{id}` |
| DELETE | `/api/v1.0/TodoPriorities/{id}` |

All protected endpoints require `Authorization: Bearer <jwt>` header.

---

## TypeScript types (src/types/index.ts)

```ts
export interface JWTResponse {
  token: string
  refreshToken: string
  firstName: string
  lastName: string
}

export interface LoginRequest { email: string; password: string }
export interface RegisterRequest { email: string; password: string; firstName: string; lastName: string }
export interface RefreshTokenRequest { jwt: string; refreshToken: string }

export interface TodoTask {
  id: string
  taskName: string
  createdDt?: string
  dueDt?: string
  isCompleted: boolean
  isArchived: boolean
  todoPriorityId?: string
  taskSort: number
  todoCategoryId?: string
  todoCategory?: TodoCategory
}

export interface TodoTaskCreate {
  taskName: string
  dueDt?: string
  isCompleted: boolean
  isArchived: boolean
  taskSort: number
  todoCategoryId?: string
  todoPriorityId?: string
}

export interface TodoCategory {
  id: string
  categoryName: string
  categorySort: number
  tag?: string
}

export interface TodoCategoryCreate { categoryName: string; categorySort: number; tag?: string }

export interface TodoPriority {
  id: string
  priorityName: string
  prioritySort: number
  syncDt?: string
}

export interface TodoPriorityCreate { priorityName: string; prioritySort: number }
```

---

## Security requirements (all mandatory)

### 1. Axios instance (`src/api/axios.ts`)

- `baseURL` reads from `import.meta.env.VITE_API_BASE_URL` with fallback to `https://taltech.akaver.com`
- **Request interceptor** (async):
  - Decode JWT payload, check if `exp - 30 < Date.now()/1000`
  - If near-expiry and refresh token exists, call RefreshToken endpoint inline, update localStorage
  - Attach `Authorization: Bearer <token>` to every request
- **Response interceptor**:
  - `403` → `router.push('/dashboard?error=forbidden')`, no refresh attempt, reject
  - `401` → if `isRefreshing` is already true, queue the request in `failedQueue`; otherwise set `isRefreshing = true`, call RefreshToken, drain queue with new token, retry original request. On failure, clear localStorage, `window.location.href = '/login'`

### 2. Auth store (`src/stores/auth.ts`)

- State: `jwt`, `refreshToken`, `firstName`, `lastName` — all initialised from `localStorage`
- `_persist(token, refresh, first, last)`: writes to both reactive refs and `localStorage`, then calls `scheduleRefresh(token)`
- `scheduleRefresh(token)`: decode `exp`, `setTimeout(() => refreshTokens(), (exp - 60) * 1000 - Date.now())`. Clear existing timer first.
- `logout()`: clear timer, clear refs, clear localStorage, manually reset all data stores (set `items=[], loading=false, error=null` on each — Pinia Composition API style stores do not have `$reset()`)
- `refreshTokens()`: returns `true` on success, calls `logout()` + returns `false` on failure

### 3. Router (`src/router/index.ts`)

- Routes without `meta: { public: true }` are protected
- `beforeEach` must be **async**:
  ```ts
  if (!to.meta.public && !auth.isAuthenticated) {
    if (localStorage.getItem('refreshToken')) {
      const ok = await auth.refreshTokens()
      if (ok) return
    }
    return '/login'
  }
  if (to.meta.public && auth.isAuthenticated) return '/dashboard'
  ```

### 4. localStorage — unavoidable constraint

The backend returns tokens in JSON body, not Set-Cookie. Tokens must go in localStorage. Compensating controls:
- **Never use `v-html`** anywhere — use `{{ }}` text interpolation only
- CSP headers in nginx.conf (see Phase 7 below)
- Keep JWT lifetime short (do not cache beyond expiry)

---

## nginx.conf (required security headers)

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self' https://taltech.akaver.com; img-src 'self' data:; frame-ancestors 'none';" always;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

`try_files ... /index.html` is required for Vue Router history mode to work on hard refresh.  
`style-src 'unsafe-inline'` is required because Vue 3 injects scoped styles at runtime.

---

## Dockerfile (multi-stage)

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_API_BASE_URL=https://taltech.akaver.com
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

---

## Pinia store pattern (all data stores follow this)

```ts
export const useTodoTasksStore = defineStore('todoTasks', () => {
  const items = ref<TodoTask[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true; error.value = null
    try { items.value = (await todoTasksApi.getAll()).data }
    catch (e) { error.value = 'Failed to load tasks' }
    finally { loading.value = false }
  }
  // create, update, remove follow same loading/error pattern

  return { items, loading, error, fetchAll, create, update, remove }
})
```

---

## Vite env variable

`VITE_API_BASE_URL` must be passed as a Docker build arg. In `vite.config.ts`, no special config needed — Vite exposes `VITE_*` variables to the client automatically.

---

## What NOT to do

- Do not use `v-html`
- Do not use the Options API — Composition API only throughout
- Do not store tokens in sessionStorage (use localStorage for cross-tab persistence)
- Do not skip the proactive expiry check in the request interceptor (it prevents double round-trips)
- Do not use `$reset()` on stores — it is only available with Options API style
- Do not add features beyond what is specified (no dark mode toggle, no drag-and-drop, no animations unless already present)
- Do not add error handling for impossible cases
- Do not add unnecessary comments — only comment non-obvious logic

---

## Documentation rule

Every phase implemented must be appended to `documentation.md` with three sections: **What**, **Why**, **How**. This is a hard requirement for the assignment.
