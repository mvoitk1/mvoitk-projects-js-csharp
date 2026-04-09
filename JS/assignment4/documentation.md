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

## Security Audit

This section documents the current security posture of the application — what is correctly implemented, what is missing or weak, and exactly how each gap should be remedied.

---

### What is currently implemented

#### JWT attachment on every request
`src/api/axios.ts` has a request interceptor that reads `jwt` from `localStorage` and sets `Authorization: Bearer <token>` on every outgoing Axios request. This means no protected API call can accidentally go out without credentials.

#### Silent refresh on 401 with request queuing
The response interceptor in `src/api/axios.ts` catches HTTP 401 responses and attempts a token refresh before giving up. It uses an `isRefreshing` flag and a `failedQueue` array to ensure that if multiple requests fail simultaneously, only one refresh call is made and all waiting requests are retried with the new token once it arrives. This correctly handles the race condition where two concurrent requests both receive a 401.

#### Route guards
`src/router/index.ts` has a `beforeEach` guard. Routes without `meta: { public: true }` redirect unauthenticated users to `/login`. Authenticated users visiting `/login` or `/register` are redirected to `/dashboard`, preventing redundant auth pages.

#### Pinia auth store with localStorage persistence
`src/stores/auth.ts` initialises reactive state from `localStorage` on store creation, so auth survives a page refresh without a server round-trip. The `_persist()` helper writes to both reactive refs and storage atomically, keeping them in sync.

#### Logout clears tokens
`logout()` removes `jwt`, `refreshToken`, `firstName`, and `lastName` from both reactive state and `localStorage`.

---

### What is missing or weak

#### 1. Tokens stored in localStorage — XSS-vulnerable

**Problem**: Any injected script (via a dependency, a DOM-based XSS, or a browser extension) can call `localStorage.getItem('jwt')` and steal the token, then make authenticated API calls from anywhere.

**Why this matters**: `localStorage` has no `httpOnly` flag — unlike cookies, the browser does not restrict JavaScript's access to it. The industry-standard fix is `httpOnly` cookies set by the server, because the browser never exposes those to JavaScript at all.

**Fix**: Since this application does not own the backend and the API sets tokens in a JSON response body (not a `Set-Cookie` header), switching to `httpOnly` cookies is not possible without backend changes. The pragmatic mitigations for this constraint are:
- Use a `sessionStorage` fallback option so tokens do not persist beyond the browser tab (reduces the window of exposure).
- Implement a strict Content Security Policy (CSP) header in `nginx.conf` to block inline scripts and restrict script sources, which makes XSS injection significantly harder.
- Keep the JWT lifetime short (the backend already does this — do not increase it client-side or cache it beyond expiry).
- Avoid calling `eval()`, `innerHTML`, `v-html`, or any raw DOM interpolation anywhere in the Vue app.

#### 2. No proactive token expiry check — always waits for a 401

**Problem**: The current flow is: make request → get 401 → attempt refresh → retry. This means every request made with an expired JWT costs two round-trips instead of one, and brief error states can flash in the UI.

**Fix**: Decode the JWT payload (it is base64-encoded, not encrypted) and check the `exp` claim before every request. If the token expires within 30 seconds, refresh it first, then make the original request. Add this to the request interceptor in `src/api/axios.ts`:

```ts
function isTokenExpiredSoon(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    // true if token expires in less than 30 seconds
    return Date.now() / 1000 > payload.exp - 30
  } catch {
    return true
  }
}
```

Then in the request interceptor, before attaching the token, check `isTokenExpiredSoon(token)` and call the refresh endpoint directly if needed.

#### 3. No proactive background refresh timer

**Problem**: If the user is idle (no API calls) for long enough that the JWT expires and the refresh token is also about to expire, the next action they take will fail and redirect them to `/login`, losing unsaved state.

**Fix**: After every successful token acquisition (login, register, or refresh), schedule a `setTimeout` that fires ~60 seconds before the JWT's `exp` claim and silently calls `refreshTokens()`. Add this inside `_persist()` in `src/stores/auth.ts`:

```ts
let refreshTimer: ReturnType<typeof setTimeout> | null = null

function scheduleRefresh(token: string) {
  if (refreshTimer) clearTimeout(refreshTimer)
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const msUntilRefresh = (payload.exp - 60) * 1000 - Date.now()
    if (msUntilRefresh > 0) {
      refreshTimer = setTimeout(() => refreshTokens(), msUntilRefresh)
    }
  } catch { /* malformed token — do nothing */ }
}
```

Call `scheduleRefresh(token)` inside `_persist()` and `clearTimeout(refreshTimer)` inside `logout()`.

#### 4. Route guard redirects to /login without attempting a silent refresh

**Problem**: On a hard page reload, the router guard runs synchronously and checks `auth.isAuthenticated`. If the JWT has expired (but the refresh token is still valid), `isAuthenticated` is `false` and the user is immediately redirected to `/login`, even though a silent refresh would have kept them logged in.

**Fix**: Make the `beforeEach` guard async. If the user is navigating to a protected route and is not currently authenticated, but a refresh token exists in `localStorage`, attempt a silent refresh before deciding to redirect. Only redirect to `/login` if the refresh fails:

```ts
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isAuthenticated) {
    if (localStorage.getItem('refreshToken')) {
      const ok = await auth.refreshTokens()
      if (ok) return // allow navigation — now authenticated
    }
    return '/login'
  }
  if (to.meta.public && auth.isAuthenticated) {
    return '/dashboard'
  }
})
```

#### 5. Logout does not reset other Pinia stores

**Problem**: After logout, `useTodoTasksStore`, `useTodoCategoryStore`, and `useTodoPriorityStore` still hold the previous user's data in memory. If a different user logs in during the same browser session, they briefly see stale data from the previous user's session before their own fetch completes.

**Fix**: Call `$reset()` on each store inside `logout()` in `src/stores/auth.ts`. For stores defined with the Composition API style (which do not get `$reset()` automatically), manually reset each reactive ref to its initial value, or switch those stores to the Options API style so Pinia generates `$reset()` automatically.

#### 6. No 403 handling — Forbidden and Unauthorised are treated the same

**Problem**: The Axios interceptor treats all non-401 errors the same. A 403 Forbidden (authenticated but not allowed to access a resource) currently bubbles up as an unhandled rejection, leaving the UI in an indeterminate state.

**Fix**: Add an explicit 403 branch in the response interceptor that does not attempt a refresh (the token is valid — the user simply lacks permission) and instead navigates to a `/forbidden` route or displays an inline error notification. The user must not be logged out on a 403.

```ts
if (error.response?.status === 403) {
  router.push({ path: '/dashboard', query: { error: 'forbidden' } })
  return Promise.reject(error)
}
```

#### 7. No Content Security Policy

**Problem**: `nginx.conf` does not set a `Content-Security-Policy` header. Without a CSP, the browser will execute any script found on the page, making XSS attacks more effective.

**Fix**: Add a restrictive CSP header to `nginx.conf`:

```nginx
add_header Content-Security-Policy
  "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self' https://taltech.akaver.com; img-src 'self' data:; frame-ancestors 'none';"
  always;
```

Also add:
```nginx
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
```

`frame-ancestors 'none'` and `X-Frame-Options: DENY` prevent clickjacking. `X-Content-Type-Options: nosniff` prevents MIME-type sniffing attacks. These headers are free to add and have no downside for an SPA.

---

### Summary table

| Issue | Severity | Fix location |
|---|---|---|
| Tokens in `localStorage` (XSS risk) | High | `nginx.conf` (CSP) + avoid `v-html` |
| No proactive expiry check before requests | Medium | `src/api/axios.ts` request interceptor |
| No background refresh timer | Medium | `src/stores/auth.ts` `_persist()` |
| Route guard does not attempt refresh on reload | Medium | `src/router/index.ts` `beforeEach` |
| Logout does not reset data stores | Low | `src/stores/auth.ts` `logout()` |
| No 403 handling | Low | `src/api/axios.ts` response interceptor |
| No security headers (CSP, X-Frame-Options) | Medium | `vue-app/nginx.conf` |

---

## Phase 8 — Docker & Deployment

**What**: `vue-app/Dockerfile` (multi-stage build), `vue-app/nginx.conf` (SPA routing), `vue-app` service added to `docker-compose.production.yml`.

**Why**: Multi-stage keeps the final image small — only the nginx alpine image ships, not Node. `try_files $uri $uri/ /index.html` is required so Vue Router's history mode works on hard refresh.

**How**: Stage 1 (`node:22-alpine`) runs `npm ci && npm run build` with `VITE_API_BASE_URL` injected as a build arg. Stage 2 (`nginx:alpine`) copies `dist/` into the nginx html root and uses a custom `nginx.conf`. The service is exposed on host port `77`, mapped to container port `80`, and joins the `infra` Docker network alongside the ASP.NET backend.
