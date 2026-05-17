# Assignment 6 — Express TodoTasks API

A small Express.js reimplementation of the subset of
`https://taltech.akaver.com` that the Vue client (`../assignment4.1`)
consumes: JWT auth (register / login / refresh) and full CRUD over
`TodoTasks`, `TodoCategories`, `TodoPriorities`. Per-user isolation,
SQLite storage. No Swagger.

## Public URLs

| Service              | Public URL                                          | VPS              |
| -------------------- | --------------------------------------------------- | ---------------- |
| Express API          | <https://mvoitk-express.proxy.itcollege.ee>         | `192.168.181.91:73` |
| Vue front (re-pointed) | <https://mvoitk-vue2.proxy.itcollege.ee>          | `192.168.181.91:76` |

The Vue client is built with `VITE_API_BASE_URL=https://mvoitk-express.proxy.itcollege.ee`
and its nginx CSP `connect-src` is updated to match.

## Endpoints

Every route is mounted under both `/api/v1/...` and `/api/v1.0/...` so
the Vue client (v1.0) and any v1 caller both work without changes.

| Method | Path                              | Auth | Notes                          |
| ------ | --------------------------------- | ---- | ------------------------------ |
| POST   | `/Account/Login`                  | no   | `{ email, password }` → tokens |
| POST   | `/Account/Register`               | no   | issues tokens immediately      |
| POST   | `/Account/RefreshToken`           | no   | `{ jwt, refreshToken }`        |
| GET    | `/TodoTasks`                      | yes  | nested category + priority     |
| GET    | `/TodoTasks/:id`                  | yes  |                                |
| POST   | `/TodoTasks`                      | yes  |                                |
| PUT    | `/TodoTasks/:id`                  | yes  |                                |
| DELETE | `/TodoTasks/:id`                  | yes  |                                |
| GET/POST/PUT/DELETE | `/TodoCategories[/:id]` | yes  | per-user                       |
| GET/POST/PUT/DELETE | `/TodoPriorities[/:id]` | yes  | per-user                       |

Auth is `Authorization: Bearer <jwt>`. Access token TTL 30 min, refresh
token TTL 30 days. Refresh tokens rotate on use (the old one is deleted).

## Stack

- Node 22, TypeScript (ESM, `NodeNext`)
- Express 4 + `cors`
- `better-sqlite3` (single file at `data/app.db`, persisted as a Docker
  volume)
- `bcryptjs` for password hashing, `jsonwebtoken` (HS256) for JWTs

## Environment

| Var               | Default              | Notes                                  |
| ----------------- | -------------------- | -------------------------------------- |
| `PORT`            | `3000`               | container port                         |
| `DB_PATH`         | `data/app.db`        | SQLite file                            |
| `JWT_SECRET`      | (auto-generated)     | overrides the on-disk secret           |
| `JWT_SECRET_PATH` | `data/.jwt_secret`   | where the auto-generated secret is kept |

If `JWT_SECRET` is unset, the server generates one on first boot and
writes it to `JWT_SECRET_PATH` so previously-issued tokens survive
restarts (as long as the data volume survives).

## Local development

```bash
npm install
npm run dev          # http://localhost:3000
```

Other scripts:

- `npm run build` — `tsc` → `dist/`
- `npm run start` — runs the built app

## Docker

```bash
docker compose up -d --build
# http://localhost:73
```

The image is multi-stage (`build` → `deps` → `runner`); the runtime
stage runs as a non-root user and writes the SQLite DB and JWT secret
to a named volume (`mvoitk-express-data`).

## VPS deployment

The container listens on `3000` inside Docker and is published on `73`
on the host. The IT College reverse proxy at
`mvoitk-express.proxy.itcollege.ee` terminates TLS and forwards to
`192.168.181.91:73`.

```bash
git pull
docker compose up -d --build
```

After deploying the API, rebuild and redeploy the Vue front so it picks
up the new `VITE_API_BASE_URL` (baked in at build time):

```bash
cd ../assignment4.1
docker compose up -d --build
```
