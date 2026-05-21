# Testing Implementation Plan — vue_front_to_PP

No tests or test tooling exist yet. This plan covers a full testing setup for the
Vue 3 + Vite + TypeScript frontend.

## 1. Tooling & setup

**Unit/component layer — Vitest + Vue Test Utils** (Vite-native; reuses `vite.config.ts` aliases):

```bash
npm i -D vitest @vue/test-utils @vitest/coverage-v8 jsdom
```

- Add `vitest.config.ts` (or merge into the vite config) with `environment: 'jsdom'`,
  `globals: true`, and a `test/setup.ts` for global stubs.
- Scripts: `"test": "vitest"`, `"test:run": "vitest run"`, `"test:cov": "vitest run --coverage"`.
- `test/setup.ts`: polyfill/stub `localStorage`, `window.dispatchEvent`/`CustomEvent`, and `fetch`
  (via a helper); `beforeEach` reset Pinia + clear localStorage + restore mocks.

**E2E layer — Playwright** (optional but recommended for auth/cart/checkout flows):

```bash
npm i -D @playwright/test && npx playwright install
```

- Run against `npm run dev` with the API mocked via `page.route('**/api/v1/**')` so tests don't
  depend on the live `mvoitk-cs3` backend.

## 2. Proposed structure

```
test/
  setup.ts
  helpers/  fetch-mock.ts, jwt.ts (build fake tokens), render.ts
src/**/__tests__/*.spec.ts   # co-located unit/component specs
e2e/*.spec.ts                # Playwright flows
```

## 3. What to test, by layer (priority order)

### A. API client — `src/api/client.ts` (highest value, most logic)
- Attaches `Authorization: Bearer` header when JWT present; omits when absent.
- Proactive refresh: expired JWT (`isJwtExpired`) triggers `ensureRefresh` before the request.
- `ensureRefresh` dedupes — N concurrent calls fire **one** `/Account/RenewRefreshToken`.
- 401 + successful refresh → retries once; 401 + failed refresh → clears tokens,
  dispatches `auth:logout`, throws.
- Error body parsing precedence: `title` → `messages[0]` → `detail` → `HTTP {status}`.
- 204 returns `undefined`; non-204 parses JSON.
- `saveTokens`/`clearTokens` dispatch `auth:tokens`.
- Mock `fetch`; assert call args + sequence.

### B. Stores (Pinia)
- `auth.ts`: `login`/`register` persist tokens; `isAdmin` decodes role from JWT (test plain `role`,
  the MS schema claim URI, and array claims); `auth:tokens`/`auth:logout` event listeners update
  state; `logout` swallows API errors and clears. Fake-time the 4-min renew interval.
- `cart.ts`: `fetchCart` toggles `loading`; `addItem`/`updateItem` replace `cart`; `removeItem`
  calls API then refetches; `itemCount` computed; `clear`.
- `locale.ts` + `useLocale().t`: ET fallback to EN when ET null/empty; `toggle`/`setLocale`
  persist to localStorage.
- `products`, `categories`, `collections`, `orders`: fetch → state mapping, error states.
  (Mock the `src/api/*` modules.)

### C. Composables
- `useToast`: `show` pushes with incrementing id + type; `dismiss` removes by id; auto-dismiss
  via fake timers.

### D. Router guards — `src/router/index.ts`
- `requiresAuth` unauthenticated → redirect to `login` with `redirect` query.
- `requiresAdmin` non-admin → redirect home; admin passes.
- Public routes unaffected.

### E. Components (Vue Test Utils, mount with stubbed stores/router)
- `ProductCard`: renders name/`lowestPrice`, localized text, link target.
- `NavBar`: language toggle calls `toggle`; admin links only when `isAdmin`; cart badge shows
  `itemCount`; login/logout state.
- `ToastContainer`: renders toasts by type, dismiss button.

### F. Views (lighter — smoke + key interactions)
- `LoginView`/`RegisterView`: submit calls store, redirects on success, shows toast on error.
- `ProductsView`: filter changes refetch with correct params.
- `CartView`/`CheckoutView`: quantity update/remove, place order.
- Admin views: form shows EN+ET inputs; CRUD calls correct API.

### G. E2E (Playwright, mocked API)
- Browse products → detail → add to cart → checkout → order confirmation.
- Login redirect flow (guarded route → login → back).
- Admin: login as admin → create/edit product.

## 4. Coverage targets & CI
- Target ~80% on `src/api`, `src/stores`, `src/composables`; lighter on views.
- Add to `npm run build` gate or a CI step: `test:run` + `test:cov`.

## 5. Suggested execution order
1. Tooling + `test/setup.ts` + helpers.
2. `client.ts` specs (A).
3. Stores + composables (B, C).
4. Router (D).
5. Components (E).
6. Views (F).
7. Playwright E2E (G).
