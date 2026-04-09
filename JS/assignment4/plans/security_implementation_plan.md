# Security Implementation Plan

This plan covers the seven security gaps identified in `documentation.md` (Security Audit section).  
Each task is self-contained and ordered from highest to lowest impact.  
All changes are additive — nothing requires touching the backend.

---

## Task 1 — Proactive JWT expiry check in Axios request interceptor

**File**: `vue-app/src/api/axios.ts`  
**Severity**: Medium  
**Depends on**: nothing

### What to do

Add a helper function that decodes the JWT payload (base64) and checks whether the token expires within the next 30 seconds. Call this helper inside the request interceptor *before* attaching the token. If the token is expiring soon and a refresh token exists, call the refresh endpoint first, wait for the new token, then attach it.

### Step-by-step

1. Add the helper above the `apiClient` definition:
   ```ts
   function isTokenExpiredSoon(token: string): boolean {
     try {
       const payload = JSON.parse(atob(token.split('.')[1]))
       return Date.now() / 1000 > payload.exp - 30
     } catch {
       return true
     }
   }
   ```

2. Change the request interceptor to async and add the check:
   ```ts
   apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
     let token = localStorage.getItem('jwt')
     const refresh = localStorage.getItem('refreshToken')

     if (token && refresh && isTokenExpiredSoon(token)) {
       try {
         const { data } = await axios.post(
           `${import.meta.env.VITE_API_BASE_URL || 'https://taltech.akaver.com'}/api/v1.0/Account/RefreshToken`,
           { jwt: token, refreshToken: refresh },
           { headers: { 'Content-Type': 'application/json' } }
         )
         token = data.token
         localStorage.setItem('jwt', data.token)
         localStorage.setItem('refreshToken', data.refreshToken)
       } catch {
         // refresh failed — let the request go through and the 401 handler will clean up
       }
     }

     if (token && config.headers) {
       config.headers['Authorization'] = `Bearer ${token}`
     }
     return config
   })
   ```

3. The existing 401 response interceptor remains unchanged as a fallback for edge cases.

### Why 30 seconds

Network round-trips and clock skew can cause a token that looks valid locally to be rejected server-side. 30 seconds is a safe buffer that avoids the "valid when sent, expired when received" failure mode.

---

## Task 2 — Proactive background refresh timer

**File**: `vue-app/src/stores/auth.ts`  
**Severity**: Medium  
**Depends on**: nothing (but works better after Task 1 is in place)

### What to do

After every successful token acquisition (login, register, or silent refresh), decode the JWT `exp` claim and schedule a `setTimeout` to call `refreshTokens()` approximately 60 seconds before the token expires. Cancel the timer on logout.

### Step-by-step

1. Add a module-level variable above the store definition:
   ```ts
   let _refreshTimer: ReturnType<typeof setTimeout> | null = null
   ```

2. Add a `scheduleRefresh` helper inside the store (not exposed in the return):
   ```ts
   function scheduleRefresh(token: string) {
     if (_refreshTimer) clearTimeout(_refreshTimer)
     try {
       const payload = JSON.parse(atob(token.split('.')[1]))
       const msUntilRefresh = (payload.exp - 60) * 1000 - Date.now()
       if (msUntilRefresh > 0) {
         _refreshTimer = setTimeout(() => refreshTokens(), msUntilRefresh)
       }
     } catch {
       // malformed token — do nothing
     }
   }
   ```

3. Call `scheduleRefresh(token)` at the end of `_persist()`:
   ```ts
   function _persist(token: string, refresh: string, first: string, last: string) {
     // ... existing lines ...
     scheduleRefresh(token)
   }
   ```

4. Cancel the timer in `logout()`:
   ```ts
   function logout() {
     if (_refreshTimer) { clearTimeout(_refreshTimer); _refreshTimer = null }
     // ... existing lines ...
   }
   ```

### Why 60 seconds before expiry

60 seconds gives enough time for the refresh request to complete and for any in-flight requests to receive the updated token from the queue. It is short enough not to waste refresh token lifetime.

---

## Task 3 — Async route guard with silent refresh on hard reload

**File**: `vue-app/src/router/index.ts`  
**Severity**: Medium  
**Depends on**: Auth store `refreshTokens()` (already exists)

### What to do

Make `beforeEach` async. If the user navigates to a protected route and `isAuthenticated` is false, but a refresh token exists in `localStorage`, attempt a silent refresh before deciding to redirect. Only redirect to `/login` if the refresh call fails.

### Step-by-step

1. Change `beforeEach` to async:
   ```ts
   router.beforeEach(async (to) => {
     const auth = useAuthStore()

     if (!to.meta.public && !auth.isAuthenticated) {
       if (localStorage.getItem('refreshToken')) {
         const ok = await auth.refreshTokens()
         if (ok) return // authenticated — allow navigation
       }
       return '/login'
     }

     if (to.meta.public && auth.isAuthenticated) {
       return '/dashboard'
     }
   })
   ```

### Why this matters

On a hard page reload, the Pinia store is freshly initialised. If the JWT has expired (even by 1 second) but the refresh token is still valid, `isAuthenticated` returns `false` and the guard immediately redirects the user to `/login`, even though they have a perfectly valid session. This fix silently restores the session before the user sees anything.

### Note on `main.ts`

If `main.ts` already calls `refreshTokens()` on startup, that call may race with the router guard on the first navigation. The guard's inline `await auth.refreshTokens()` is safe because `refreshTokens()` is idempotent — calling it twice in quick succession will not cause a double-refresh (the second call will use the tokens the first already stored).

---

## Task 4 — Reset data stores on logout

**File**: `vue-app/src/stores/auth.ts` (and verify store definitions)  
**Severity**: Low  
**Depends on**: nothing

### What to do

After clearing auth tokens, call `$reset()` (or manual state reset) on every data store so that stale data from the previous user's session is not visible to the next user who logs in on the same browser tab.

### Step-by-step

1. Import the data stores at the top of `auth.ts`:
   ```ts
   import { useTodoTasksStore } from './todoTasks'
   import { useTodoCategoryStore } from './todoCategory'
   import { useTodoPriorityStore } from './todoPriority'
   ```

2. Add resets at the end of `logout()`:
   ```ts
   function logout() {
     // ... existing token-clearing lines ...
     useTodoTasksStore().$reset()
     useTodoCategoryStore().$reset()
     useTodoPriorityStore().$reset()
   }
   ```

3. **Verify**: `$reset()` is only automatically generated for stores using the **Options API** style (`defineStore('id', { state: () => ({...}), ... })`). If any store uses the **Composition API** style (`defineStore('id', () => { ... })`), `$reset()` does not exist. In that case, either:
   - Switch that store to Options API style, or
   - Manually reset each ref: `items.value = []; loading.value = false; error.value = null`

---

## Task 5 — Handle 403 Forbidden separately from 401 Unauthorized

**File**: `vue-app/src/api/axios.ts`  
**Severity**: Low  
**Depends on**: Vue Router instance accessible from axios.ts

### What to do

Add a dedicated 403 branch in the response interceptor that does NOT attempt a refresh (the token is valid — the user lacks permission) and instead redirects to `/dashboard` with an error query param, or shows a toast notification.

### Step-by-step

1. Import the router at the top of `axios.ts`:
   ```ts
   import router from '../router'
   ```

2. Add a 403 check before the 401 check in the response interceptor:
   ```ts
   if (error.response?.status === 403) {
     router.push({ path: '/dashboard', query: { error: 'forbidden' } })
     return Promise.reject(error)
   }
   ```

3. In `DashboardView.vue`, read the `error` query param on mount and show a message:
   ```ts
   import { useRoute } from 'vue-router'
   const route = useRoute()
   // In template: <p v-if="route.query.error === 'forbidden'">You don't have permission to access that resource.</p>
   ```

### Why not logout on 403

A 403 means the user is authenticated but not authorised for a specific resource. Logging them out would be incorrect and disruptive. The right response is to inform them and keep them in the app.

---

## Task 6 — Security headers in nginx.conf

**File**: `vue-app/nginx.conf`  
**Severity**: Medium  
**Depends on**: nothing (pure configuration change)

### What to do

Add HTTP security headers to the nginx config. These headers instruct the browser to enforce restrictions that make XSS, clickjacking, and MIME-sniffing attacks harder or impossible.

### Step-by-step

Add the following lines inside the `server` block, before the `location` blocks:

```nginx
# Prevent the page from being loaded in an iframe (clickjacking)
add_header X-Frame-Options "DENY" always;

# Prevent browsers from MIME-type sniffing
add_header X-Content-Type-Options "nosniff" always;

# Reduce referrer information sent to external sites
add_header Referrer-Policy "strict-origin-when-cross-origin" always;

# Content Security Policy — restrict what can execute on this page
add_header Content-Security-Policy
  "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self' https://taltech.akaver.com; img-src 'self' data:; frame-ancestors 'none';"
  always;
```

### Header-by-header explanation

| Header | Effect |
|---|---|
| `X-Frame-Options: DENY` | Blocks all iframe embedding — prevents clickjacking |
| `X-Content-Type-Options: nosniff` | Browser will not guess MIME type — prevents script injection via mistyped resources |
| `Referrer-Policy` | Sends only origin (not full path) to cross-origin requests — reduces data leakage |
| `Content-Security-Policy` | Whitelist of allowed content sources — the strongest XSS mitigation available at the HTTP layer |

### CSP notes

- `script-src 'self'` — only scripts from the same origin are allowed. No inline `<script>` tags, no `eval()`, no CDN scripts unless explicitly added.
- `style-src 'self' 'unsafe-inline'` — Vue component `<style>` blocks compile to inline styles; `unsafe-inline` is required for them. This is a known trade-off for Vue SPA without a nonce-based CSP.
- `connect-src 'self' https://taltech.akaver.com` — the only external host the app may call is the API backend.
- `frame-ancestors 'none'` — equivalent to `X-Frame-Options: DENY` but enforced by CSP-aware browsers.

---

## Task 7 — Reduce localStorage exposure (defence-in-depth)

**File**: `vue-app/src/stores/auth.ts`  
**Severity**: High (awareness) / Low (what we can actually fix without a backend change)  
**Depends on**: nothing

### What to do

Since the backend returns tokens in the JSON body (not `Set-Cookie`), true `httpOnly` cookie storage is not possible. The mitigations available client-side are:

1. **Avoid `v-html`**: Audit all `.vue` files for `v-html` usage. Every `v-html` binding is a potential XSS vector that could allow a script to read `localStorage`. Replace all `v-html` uses with safe alternatives (`v-text`, computed properties, or sanitised rendering).

2. **Keep JWT lifetime short**: Do not extend token lifetime client-side. The shorter the JWT lifetime, the smaller the window of exposure if a token is stolen.

3. **Do not log tokens**: Ensure no `console.log`, error reporting service, or analytics tool receives the JWT or refresh token as a string.

4. **Document the limitation**: Add a comment in `auth.ts` acknowledging the `localStorage` risk and the constraint that prevents using `httpOnly` cookies:
   ```ts
   // NOTE: Tokens are stored in localStorage because the backend returns them
   // in the JSON response body and does not support Set-Cookie / httpOnly.
   // The CSP headers in nginx.conf and the absence of v-html are the primary
   // XSS mitigations in place of httpOnly cookie storage.
   ```

### If backend access becomes available

If the backend is ever accessible for modification, the correct fix is:
1. Server sets `Set-Cookie: refreshToken=<value>; httpOnly; Secure; SameSite=Strict`
2. Client never reads or stores the refresh token — the browser handles it automatically
3. The JWT can remain in memory (a Pinia ref without localStorage persistence) since it is short-lived
4. On page reload, the app calls the refresh endpoint — the browser sends the cookie automatically, returning a new JWT

---

## Implementation order

| # | Task | File(s) | Risk |
|---|---|---|---|
| 1 | Proactive expiry check in request interceptor | `api/axios.ts` | Low — additive change |
| 2 | Background refresh timer | `stores/auth.ts` | Low — additive change |
| 3 | Async route guard with silent refresh | `router/index.ts` | Low — behaviour improvement |
| 4 | Reset stores on logout | `stores/auth.ts` | Low — additive calls |
| 5 | Handle 403 in Axios interceptor | `api/axios.ts` | Low — new branch |
| 6 | Security headers in nginx.conf | `nginx.conf` | Low — config only |
| 7 | localStorage audit + comment | `stores/auth.ts` + all `.vue` files | Low — no logic change |

Tasks 1–4 can be done in a single commit. Tasks 5–6 are independent and can be done separately. Task 7 is an audit + comment, not a code change.
