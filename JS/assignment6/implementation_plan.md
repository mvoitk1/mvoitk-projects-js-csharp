# Assignment 6 — Implementation Plan

Reimplement the subset of `https://taltech.akaver.com` that the Vue client
(`../assignment4.1`) consumes, ship it in Docker, and re-point the Vue front
at the new backend.

The React/Next client (`../assignment5`) is **out of scope** for this
assignment — only the Vue client gets re-pointed.

## 1. Scope of the API surface

The Vue client (`assignment4.1/src/api/*.ts`) calls these paths:

| Method | Path                                  | Purpose                          |
| ------ | ------------------------------------- | -------------------------------- |
| POST   | `/api/v1.0/Account/Login`             | email+password → JWT + refresh   |
| POST   | `/api/v1.0/Account/Register`          | create user → JWT + refresh      |
| POST   | `/api/v1.0/Account/RefreshToken`      | swap `{jwt, refreshToken}` pair  |
| GET    | `/api/v1.0/TodoTasks`                 | list (current user)              |
| GET    | `/api/v1.0/TodoTasks/:id`             | get one                          |
| POST   | `/api/v1.0/TodoTasks`                 | create                           |
| PUT    | `/api/v1.0/TodoTasks/:id`             | update                           |
| DELETE | `/api/v1.0/TodoTasks/:id`             | delete                           |
| (same CRUD)  | `/api/v1.0/TodoCategories`      |                                  |
| (same CRUD)  | `/api/v1.0/TodoPriorities`      |                                  |

We **also** mount the same routers under `/api/v1/...` so the React client
(or any other v1 caller) keeps working without modification. The Vue
client will not be touched on that front.

Swagger is explicitly not needed.

## 2. Tech stack

- Node 22 + TypeScript (ESM, `NodeNext`), already scaffolded.
- Express 4 (downgrading from the placeholder Express 5 in `package.json`
  for stability with current types).
- `better-sqlite3` — single-file SQLite DB. Synchronous, perfect fit for
  this size of workload, no async pool to wrangle.
- `bcryptjs` — pure-JS bcrypt; avoids native-compile pain in Alpine.
- `jsonwebtoken` — HS256-signed access tokens (~30 min) + opaque
  refresh tokens stored as bcrypt hashes in the DB.
- `cors`, `dotenv`.

## 3. Data model (SQLite)

```sql
users (
  id           TEXT PRIMARY KEY,   -- uuid
  email        TEXT UNIQUE NOT NULL,
  passwordHash TEXT NOT NULL,
  firstName    TEXT,
  lastName     TEXT,
  createdDt    TEXT NOT NULL
);

refresh_tokens (
  id         TEXT PRIMARY KEY,
  userId     TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  tokenHash  TEXT NOT NULL,
  jwtHash    TEXT NOT NULL,        -- the JWT it was issued alongside
  expiresAt  TEXT NOT NULL,
  createdDt  TEXT NOT NULL
);

todo_categories (
  id           TEXT PRIMARY KEY,
  userId       TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  categoryName TEXT,
  categorySort INTEGER NOT NULL DEFAULT 0,
  tag          TEXT,
  syncDt       TEXT NOT NULL
);

todo_priorities (
  id           TEXT PRIMARY KEY,
  userId       TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  priorityName TEXT,
  prioritySort INTEGER NOT NULL DEFAULT 0,
  syncDt       TEXT NOT NULL
);

todo_tasks (
  id              TEXT PRIMARY KEY,
  userId          TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  taskName        TEXT,
  taskSort        INTEGER NOT NULL DEFAULT 0,
  createdDt       TEXT NOT NULL,
  dueDt           TEXT,
  isCompleted     INTEGER NOT NULL DEFAULT 0,
  isArchived      INTEGER NOT NULL DEFAULT 0,
  todoCategoryId  TEXT REFERENCES todo_categories(id) ON DELETE SET NULL,
  todoPriorityId  TEXT REFERENCES todo_priorities(id) ON DELETE SET NULL,
  syncDt          TEXT NOT NULL
);
```

All `Todo*` tables are scoped per user — a request only sees its own rows.
DB file lives at `data/app.db`, mounted as a Docker volume so it survives
container rebuilds.

## 4. Auth flow

- `POST /Account/Register`: hash password (bcrypt cost 10), insert user,
  immediately issue tokens (mirrors taltech behavior the Vue client
  expects).
- `POST /Account/Login`: verify bcrypt, issue tokens. 404 on bad
  credentials (matches the taltech error shape the React client checks
  for; harmless for Vue).
- `POST /Account/RefreshToken`: accepts `{ jwt, refreshToken }`. Locate
  the refresh row by hashing `refreshToken`, confirm it was paired with
  this exact `jwt`, confirm not expired. Issue new pair, **delete the
  old refresh row** (rotation).
- Bearer guard middleware on all `/TodoTasks`, `/TodoCategories`,
  `/TodoPriorities` routes. Failure → 401 with empty body (cheap; clients
  retry via refresh).

Token shape returned to the client (matches `JWTResponse` /
`AccountTokenResponse`):

```json
{ "token": "<jwt>", "refreshToken": "<opaque>", "firstName": "...", "lastName": "..." }
```

JWT payload: `{ sub: userId, email }`, 30-minute expiry. Refresh token:
32 random bytes → base64url, 30-day expiry.

`JWT_SECRET` comes from env; the container generates one at first boot
if unset and writes it to the data volume so restarts don't invalidate
issued tokens.

## 5. Response shape for TodoTasks

`GET /TodoTasks` returns the row plus nested `todoCategory` and
`todoPriority` objects (or `null`) so the Vue list view can display
category names without an extra round-trip. Single-task GET does the
same. POST/PUT echo back the same shape.

`syncDt` is set to `new Date().toISOString()` on insert and update.

## 6. File layout

```
src/
  index.ts            # entrypoint (existing)
  app.ts              # express setup, mounts both prefixes
  db.ts               # better-sqlite3 setup + migrations + prepared stmts
  auth.ts             # bcrypt + jwt helpers + bearer middleware
  routes/
    account.ts        # Login / Register / RefreshToken
    tasks.ts          # rewrite of the placeholder
    categories.ts
    priorities.ts
```

`src/app.ts` mounts every router twice:

```ts
app.use("/api/v1/TodoTasks", tasksRouter);
app.use("/api/v1.0/TodoTasks", tasksRouter);
// etc.
```

Account routes likewise on `/api/v1/Account` + `/api/v1.0/Account`.

## 7. Docker

- Multi-stage Dockerfile: `node:22-alpine` build stage runs `npm ci` +
  `npm run build`, runtime stage copies `dist/`, prod `node_modules`,
  and runs `node dist/index.js` as non-root.
- `better-sqlite3` is native — install build-base in the build stage so
  the prebuilt or recompiled binary makes it into `node_modules`.
- `docker-compose.yml` publishes `73:3000`, mounts a named volume on
  `/app/data` for the SQLite file, joins the existing external `infra`
  network so the IT College proxy reaches it the same way the Vue
  container does.

## 8. Re-pointing the Vue front

In `../assignment4.1/docker-compose.yml`, swap the build arg:

```yaml
VITE_API_BASE_URL: https://mvoitk-express.proxy.itcollege.ee
```

In `../assignment4.1/nginx.conf` extend the CSP `connect-src` to include
the new origin (replacing or augmenting `https://taltech.akaver.com`).

That's the only client-side change required — all path prefixes
(`/api/v1.0/...`) stay the same.

## 9. README

Replace the placeholder README with:
- one-line description,
- public URLs:
  - API: `https://mvoitk-express.proxy.itcollege.ee` → `192.168.181.91:73`
  - Vue front (re-pointed): `https://mvoitk-vue2.proxy.itcollege.ee` →
    `192.168.181.91:76`
- local dev quickstart (`npm run dev`),
- Docker quickstart (`docker compose up -d --build`),
- env vars (`PORT`, `JWT_SECRET`, `DB_PATH`),
- short API reference table.

## 10. Out of scope / non-goals

- No Swagger / OpenAPI doc generation.
- No password reset, email verification, role-based auth.
- No pagination / filtering on list endpoints — the Vue client fetches
  everything.
- React/Next client (assignment5) is not touched.
- No DB migrations framework — schema is created idempotently on boot
  via `CREATE TABLE IF NOT EXISTS`.
