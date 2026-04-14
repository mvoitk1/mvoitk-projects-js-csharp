# Implementation Plan — Assignment 4.1 Vue Frontend

Vue 3 SPA with JWT auth against `https://taltech.akaver.com/`, covering ToDo tasks, categories, and priorities.

---

## Overview

| Phase | What | Files |
|---|---|---|
| 1 | Install deps + types | `package.json`, `src/types/index.ts` |
| 2 | API layer | `src/api/axios.ts`, `src/api/auth.ts`, `src/api/*.ts` |
| 3 | Auth store | `src/stores/auth.ts` |
| 4 | Data stores | `src/stores/todoTasks.ts`, `todoCategory.ts`, `todoPriority.ts` |
| 5 | Router + guards | `src/router/index.ts` |
| 6 | Views + components | All `src/views/*.vue`, `src/components/NavBar.vue` |
| 7 | Docker + nginx | `Dockerfile`, `nginx.conf`, `docker-compose.yml` |
| 8 | CI/CD | `.gitlab-ci.yml` new deploy job |

---

## Phase 1 — Install dependencies & define types

**Tasks:**
1. `npm install axios` — the only missing dependency
2. Create `src/types/index.ts` with all interfaces (see CLAUDE.md for full list):
   - `JWTResponse`, `LoginRequest`, `RegisterRequest`, `RefreshTokenRequest`
   - `TodoTask`, `TodoTaskCreate`
   - `TodoCategory`, `TodoCategoryCreate`
   - `TodoPriority`, `TodoPriorityCreate`

**Done when:** TypeScript compiles without errors after adding types.

---

## Phase 2 — API layer

### 2a. `src/api/axios.ts` — Axios instance with interceptors

This is the most critical file. Build it carefully.

**Request interceptor (async):**
1. Read `jwt` and `refreshToken` from `localStorage`
2. If token exists, decode base64 payload, check `exp - 30 < Date.now()/1000`
3. If near-expiry and refresh token present, call `POST /api/v1.0/Account/RefreshToken` inline using a plain `axios.post` (not the instance, to avoid interceptor loops)
4. On success, write new tokens to localStorage
5. Attach `Authorization: Bearer <token>` header

**Response interceptor:**
1. On `403`: push to `/dashboard?error=forbidden`, reject without retry
2. On `401` (and `!originalRequest._retry`):
   - If `isRefreshing === true`: push to `failedQueue`, return queued promise
   - Otherwise: set `isRefreshing = true`, call RefreshToken, drain queue, retry original
   - On refresh failure: clear localStorage, `window.location.href = '/login'`

### 2b. API wrappers

`src/api/auth.ts`:
```ts
export const authApi = {
  login: (data: LoginRequest) => apiClient.post<JWTResponse>('/api/v1.0/Account/Login', data),
  register: (data: RegisterRequest) => apiClient.post<JWTResponse>('/api/v1.0/Account/Register', data),
  refreshToken: (data: RefreshTokenRequest) => apiClient.post<JWTResponse>('/api/v1.0/Account/RefreshToken', data),
}
```

`src/api/todoTasks.ts`, `todoCategory.ts`, `todoPriority.ts` — standard CRUD wrappers (`getAll`, `getById`, `create`, `update`, `remove`) using `apiClient`.

**Done when:** API wrappers are typed, no `any` usage.

---

## Phase 3 — Auth store

**File:** `src/stores/auth.ts`

State (all init from localStorage):
- `jwt`, `refreshToken`, `firstName`, `lastName`

Computed:
- `isAuthenticated`: `!!jwt.value`
- `currentUser`: `firstName lastName`

Private helpers:
- `_persist(token, refresh, first, last)`: write to refs + localStorage + call `scheduleRefresh(token)`
- `scheduleRefresh(token)`: clear existing `_refreshTimer`, decode `exp`, set `setTimeout` for `(exp - 60) * 1000 - Date.now()` ms

Actions:
- `login(credentials)`: call `authApi.login`, pass to `_persist`
- `register(info)`: call `authApi.register`, pass to `_persist`
- `refreshTokens()`: call `authApi.refreshToken` with current tokens, on success `_persist`, return `true`; on failure call `logout()`, return `false`
- `logout()`: clear timer, clear refs, clear localStorage, reset all data stores manually

**Done when:** Login persists across page refresh; logout clears all state.

---

## Phase 4 — Data stores

Three stores, identical pattern:

**`src/stores/todoTasks.ts`** (same for category, priority):
- State: `items: ref<TodoTask[]>([])`, `loading: ref(false)`, `error: ref<string|null>(null)`
- `fetchAll()`: set loading, call API, set items, catch → set error, finally clear loading
- `create(data)`: call API, push to items
- `update(id, data)`: call API, splice into items
- `remove(id)`: call API, filter from items

**Done when:** Data is shared between views via store, no redundant API calls on route change.

---

## Phase 5 — Router + navigation guards

**File:** `src/router/index.ts`

Routes:
```
/              → redirect /dashboard
/login         → LoginView       { meta: { public: true } }
/register      → RegisterView    { meta: { public: true } }
/dashboard     → DashboardView
/tasks/new     → CreateTaskView
/todos/:id     → TodoDetailView
/categories    → CategoryListView
/priorities    → PriorityListView
```

`beforeEach` guard (must be `async`):
```ts
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isAuthenticated) {
    if (localStorage.getItem('refreshToken')) {
      const ok = await auth.refreshTokens()
      if (ok) return
    }
    return '/login'
  }
  if (to.meta.public && auth.isAuthenticated) return '/dashboard'
})
```

**Done when:** Hard page reload on a protected route silently refreshes and stays on the page.

---

## Phase 6 — Views and components

### `src/App.vue`
```vue
<template>
  <NavBar v-if="auth.isAuthenticated" />
  <RouterView />
</template>
```

### `src/components/NavBar.vue`
Links: Dashboard, Categories, Priorities, Logout button (calls `auth.logout()` then `router.push('/login')`)

### `src/views/LoginView.vue`
- Form: email, password
- On submit: `auth.login(credentials)` → push `/dashboard`
- Show error message on failure
- Link to `/register`

### `src/views/RegisterView.vue`
- Form: email, password, firstName, lastName
- On submit: `auth.register(info)` → push `/dashboard`
- Link to `/login`

### `src/views/DashboardView.vue`
- `onMounted`: `tasksStore.fetchAll()` (also fetch categories + priorities for dropdowns)
- Show `route.query.error === 'forbidden'` warning (from 403 interceptor)
- List tasks: show taskName, completion status, category, due date
- Inline toggle `isCompleted` (PUT)
- Delete button per task
- "New task" link → `/tasks/new`

### `src/views/CreateTaskView.vue`
- Form: taskName (required), dueDt, todoCategoryId (select), todoPriorityId (select), taskSort
- On submit: `tasksStore.create(data)` → push `/dashboard`

### `src/views/TodoDetailView.vue`
- Load task by `route.params.id` on mount
- Editable form with all fields
- Save → `tasksStore.update(id, data)`
- Delete → `tasksStore.remove(id)` → push `/dashboard`

### `src/views/CategoryListView.vue`
- List categories: name, sort, tag
- Inline create form (name, sort, tag)
- Delete per category

### `src/views/PriorityListView.vue`
- Same pattern as CategoryListView for priorities

**Done when:** Full CRUD works for tasks, categories, priorities in the browser.

---

## Phase 7 — Docker and nginx

### `nginx.conf`

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

### `Dockerfile`

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

### `docker-compose.yml`

```yaml
services:
  vue-app-4-1:
    build:
      context: .
      args:
        VITE_API_BASE_URL: https://taltech.akaver.com
    container_name: vue-app-taltech-4-1
    restart: unless-stopped
    ports:
      - "76:80"
    networks:
      - infra

networks:
  infra:
    name: infra
    external: true
```

**Note:** Deployed at `mvoitk-vue2.proxy.itcollege.ee` → `192.168.181.91:76`.

**Done when:** `docker compose up --build` serves the app at `http://localhost:78`.

---

## Phase 8 — CI/CD

Add to `.gitlab-ci.yml` at repo root:

```yaml
deploy_javascript_a4_1:
  stage: deploy
  only:
    - main
  tags:
    - shared
  script:
    - docker network create infra || true
    - cd JS/assignment4.1
    - docker compose -p jsassignment4_1 up --build --remove-orphans --detach
```

**Done when:** Push to `main` triggers deployment and the app is reachable at the VPS public URL.

---

## Deployment details

- **Port**: `76` on the VPS (`192.168.181.91:76`)
- **Public URL**: `https://mvoitk-vue2.proxy.itcollege.ee`
- **README**: must list this URL as required by the assignment
- **CI**: add a new `deploy_javascript_a4_1` job — do not touch the existing `deploy_javascript_a4` job

---

## Documentation requirement

After completing each phase, append an entry to `documentation.md` with:
- **What**: what was built
- **Why**: why this approach
- **How**: how it works

This is mandatory per assignment specification.

---

## Security checklist

Before submitting:
- [ ] No `v-html` in any `.vue` file
- [ ] Proactive JWT expiry check in request interceptor
- [ ] Background refresh timer (`scheduleRefresh`) active
- [ ] `beforeEach` guard is async and attempts silent refresh
- [ ] `logout()` resets all data stores
- [ ] 403 handled separately from 401
- [ ] nginx CSP + X-Frame-Options + X-Content-Type-Options headers present
- [ ] `VITE_API_BASE_URL` passed as Docker build arg, not hardcoded
