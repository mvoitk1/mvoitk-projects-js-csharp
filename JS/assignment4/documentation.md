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

## Phase 9 — Security Hardening Implementation

This phase implements all seven gaps identified in the Security Audit section above. Each fix is documented individually.

---

### Fix 1 — Proactive JWT expiry check in Axios request interceptor

**File**: `vue-app/src/api/axios.ts`

**What**: Added a `isTokenExpiredSoon(token)` helper and made the request interceptor `async`. Before attaching the JWT to any outgoing request, the interceptor decodes the token's base64 payload, reads the `exp` claim, and checks whether the token expires within the next 30 seconds. If it does — and a refresh token is present — a refresh call is made inline before the original request continues.

**Why**: The previous flow was reactive: make request → receive 401 → attempt refresh → retry. This meant every request made after a token expired cost two HTTP round-trips and could cause brief error flashes in the UI. By checking `exp` proactively, the original request goes out with a valid token the first time. The 30-second buffer accounts for network latency and clock skew between the client and server — a token that appears valid locally can arrive expired at the server if there is even a small delay.

**How**: `isTokenExpiredSoon` splits the JWT on `.`, takes the second segment (the payload), decodes it with `atob()`, parses the JSON, and compares `Date.now() / 1000` against `payload.exp - 30`. The function returns `true` on any error (malformed token, missing claim) so that a broken token always triggers a refresh attempt rather than being attached blindly. The interceptor was converted from synchronous to `async` so the refresh `await` is legal. If the inline refresh fails, the catch block is silent and the original (near-expired) token is still attached — the existing 401 fallback then handles the failure.

---

### Fix 2 — Background refresh timer

**File**: `vue-app/src/stores/auth.ts`

**What**: Added a module-level `_refreshTimer` variable and a `scheduleRefresh(token)` helper. After every successful token acquisition — login, register, or silent refresh — `scheduleRefresh` is called from `_persist()`. It decodes the new JWT's `exp` claim and sets a `setTimeout` to fire `refreshTokens()` approximately 60 seconds before the token would expire. The timer is cancelled in `logout()`.

**Why**: Without this, a user who is idle (no API calls) long enough for the JWT to expire will have their next action fail and get redirected to `/login`, losing any unsaved UI state. Fix 1 catches the case where a request is made near expiry, but it cannot help if the user simply sits on a page without making requests. The background timer ensures tokens are always kept fresh as long as the user has the tab open.

**Why 60 seconds**: This gives enough time for the refresh HTTP request to complete and for Fix 1's 30-second buffer to kick in if somehow the timer fires late. The two values are complementary: the timer keeps the token fresh during idle periods; the request-interceptor check is a last-resort safety net for edge cases.

**How**: `scheduleRefresh` clears any existing timer first (preventing duplicate timers when tokens are refreshed multiple times in a session), parses `exp` from the JWT payload using the same `atob` approach as Fix 1, computes `msUntilRefresh = (payload.exp - 60) * 1000 - Date.now()`, and calls `setTimeout` if the result is positive. A negative value means the token is already expired or within 60 seconds of expiry — in that case no timer is set and Fix 1 handles the next request. `logout()` calls `clearTimeout(_refreshTimer)` and nulls the reference so the timer cannot fire after the user is logged out.

---

### Fix 3 — Async route guard with silent refresh on hard reload

**File**: `vue-app/src/router/index.ts`

**What**: Changed `beforeEach` from a synchronous arrow function to an `async` function. If the guard would have redirected to `/login` (protected route, `isAuthenticated` is false), it now first checks whether a refresh token exists in `localStorage`. If one does, it awaits `auth.refreshTokens()`. If the refresh succeeds, the navigation is allowed to proceed. Only if the refresh fails (or there is no refresh token) does the guard redirect to `/login`.

**Why**: On a hard page reload, the Pinia store is freshly initialised with whatever is in `localStorage`. If the JWT has expired — even by one second — `isAuthenticated` is `false` and the old synchronous guard immediately redirected the user to `/login`, forcing a new login even though the refresh token was perfectly valid. This was the most disruptive UX failure: a user returning to the app after a few hours would be logged out despite never explicitly logging out.

**How**: `auth.refreshTokens()` in the auth store already returns `true` on success and `false` on failure (calling `logout()` internally on failure). The guard awaits this boolean and uses it to branch: `if (ok) return` allows the original navigation to complete; otherwise `return '/login'` redirects. The function is idempotent — if `main.ts` also calls `refreshTokens()` on startup, the two calls may race on the first navigation but the second call will simply use the tokens the first already stored.

---

### Fix 4 — Reset data stores on logout

**File**: `vue-app/src/stores/auth.ts`

**What**: `logout()` now manually resets the state of `useTodoTasksStore`, `useTodoCategoryStore`, and `useTodoPriorityStore` by setting `items = []`, `loading = false`, and `error = null` on each.

**Why**: After logout, all three data stores still held the previous user's tasks, categories, and priorities in memory. If a different user logged in on the same browser session, they would see the previous user's data flash briefly before their own `fetchAll()` completed. This is a data isolation failure — one user's data must never be visible to another.

**Why not `$reset()`**: Pinia only auto-generates `$reset()` for stores defined with the Options API style (`defineStore('id', { state: () => ({}) })`). All three data stores use the Composition API style (`defineStore('id', () => {})`), so `$reset()` does not exist on them. The manual reset of each ref is equivalent in effect.

**How**: The three store composables are imported at the top of `auth.ts`. Inside `logout()`, after clearing auth state and localStorage, each store is instantiated via its composable and each state ref is set to its initial value. Pinia reactivity ensures that any component currently subscribed to those stores immediately sees the cleared state.

---

### Fix 5 — Handle 403 Forbidden separately from 401 Unauthorized

**File**: `vue-app/src/api/axios.ts`  
**File**: `vue-app/src/views/DashboardView.vue`

**What**: Added an explicit `403` branch in the Axios response interceptor that pushes to `/dashboard?error=forbidden` via Vue Router, then rejects the promise without attempting a token refresh. Added a conditional error message in `DashboardView.vue` that reads `route.query.error` and displays a human-readable explanation when the value is `'forbidden'`.

**Why**: HTTP 401 (Unauthorized) means the request lacks valid credentials — the token is missing, expired, or invalid. HTTP 403 (Forbidden) means the token is valid but the authenticated user does not have permission to access that specific resource. These are fundamentally different situations. Treating 403 like 401 — attempting a token refresh — is incorrect: a refresh will produce another valid token for the same user, who still lacks the permission. Worse, if the refresh fails, the user gets logged out, which is actively misleading ("you need to log in again" when the real issue is "you are not allowed here").

**How**: The 403 check is placed before the 401 check in the interceptor so it is evaluated first. `router.push({ path: '/dashboard', query: { error: 'forbidden' } })` sends the user to a page they can always reach and passes the error reason as a URL query param (state-free, survives page refresh, avoids flash-of-empty). In `DashboardView.vue`, `useRoute()` is imported and instantiated, and a `<p v-if="route.query.error === 'forbidden'">` element is shown above the task error, styled with the existing `.error` class.

---

### Fix 6 — Security headers in nginx.conf

**File**: `vue-app/nginx.conf`

**What**: Added four HTTP response headers to the nginx `server` block:
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self' https://taltech.akaver.com; img-src 'self' data:; frame-ancestors 'none';`

**Why**: The app had no HTTP security headers at all. These headers are enforced by the browser on every page load — they cost nothing at runtime and provide significant protection against common attack classes:

- `X-Frame-Options: DENY` — prevents the page from being embedded in an `<iframe>` on a malicious site, blocking clickjacking attacks where a hidden iframe tricks users into clicking UI elements they cannot see.
- `X-Content-Type-Options: nosniff` — prevents the browser from guessing (`sniffing`) the MIME type of a response and executing it as a different type. Without this, a maliciously uploaded file served with a permissive MIME type could be executed as JavaScript.
- `Referrer-Policy: strict-origin-when-cross-origin` — sends the full URL as `Referer` on same-origin navigations (useful for analytics) but sends only the origin (e.g. `https://mvoitk-vue.proxy.itcollege.ee`) on cross-origin requests. This prevents API URLs, query parameters, or path fragments from leaking to third-party servers.
- `Content-Security-Policy` — the primary XSS mitigation at the HTTP layer. `script-src 'self'` blocks all inline `<script>` tags and any script loaded from an external origin, which closes the most common XSS escalation path. `connect-src 'self' https://taltech.akaver.com` ensures the only external host the app can call is the known backend. `frame-ancestors 'none'` duplicates `X-Frame-Options: DENY` for CSP-aware browsers.

**Why `style-src 'unsafe-inline'`**: Vue 3 single-file components compile `<style>` blocks into inline styles injected at runtime by the framework. There is no way to use a CSP nonce with Vite's CSS injection without a custom server-side nonce setup. `'unsafe-inline'` for styles is a known, accepted trade-off for Vue SPAs — it weakens style-based injection protection but does not affect script execution.

**How**: All four `add_header` directives are placed inside the `server` block, before the `location` blocks, so they apply to all responses including the `index.html` entry point and all static assets. The `always` flag ensures headers are sent even on error responses (4xx, 5xx), which matters for 403/404 pages that could otherwise be framed or sniffed.

---

### Fix 7 — Reduce localStorage exposure (defence-in-depth)

**File**: `vue-app/src/stores/auth.ts` (comment), all `.vue` files (audit)

**What**: Added a code comment in `auth.ts` above `_persist()` documenting the constraint that forces token storage in `localStorage` and referencing the compensating controls. Audited all `.vue` files for `v-html` usage — none found.

**Why**: `localStorage` is readable by any JavaScript running on the same origin. Unlike `httpOnly` cookies — which the browser withholds from JavaScript entirely — a successful XSS attack can call `localStorage.getItem('jwt')` and exfiltrate both tokens. The ideal fix (server-set `httpOnly` cookies) requires backend changes that are out of scope here because this app does not own the API at `taltech.akaver.com`.

The comment serves two purposes: it explains to future maintainers why the security-conscious choice (httpOnly cookie) was not made, so they do not assume it was an oversight; and it points to the compensating controls (CSP headers from Fix 6, absence of `v-html`) that reduce the XSS surface area.

**`v-html` audit result**: Zero uses of `v-html` found across all `.vue` files. Every dynamic binding in the app uses `{{ }}` text interpolation or `v-text`, both of which Vue HTML-escapes automatically. This is the correct posture — it eliminates the most common DOM-based XSS vector in Vue applications.

**What would be needed for httpOnly cookie storage**: If backend access became available, the correct architecture would be: (1) server sets `Set-Cookie: refreshToken=<value>; httpOnly; Secure; SameSite=Strict` — the browser handles the cookie entirely, JavaScript never sees it; (2) the JWT is kept only in a Pinia `ref` (memory, not localStorage) since it is short-lived; (3) on page reload, the app calls the refresh endpoint — the browser automatically sends the httpOnly cookie, returning a new JWT without any localStorage read. This eliminates the `localStorage` attack surface entirely.

---

## Phase 8 — Docker & Deployment

**What**: `vue-app/Dockerfile` (multi-stage build), `vue-app/nginx.conf` (SPA routing), `vue-app` service added to `docker-compose.production.yml`.

**Why**: Multi-stage keeps the final image small — only the nginx alpine image ships, not Node. `try_files $uri $uri/ /index.html` is required so Vue Router's history mode works on hard refresh.

**How**: Stage 1 (`node:22-alpine`) runs `npm ci && npm run build` with `VITE_API_BASE_URL` injected as a build arg. Stage 2 (`nginx:alpine`) copies `dist/` into the nginx html root and uses a custom `nginx.conf`. The service is exposed on host port `77`, mapped to container port `80`, and joins the `infra` Docker network alongside the ASP.NET backend.
