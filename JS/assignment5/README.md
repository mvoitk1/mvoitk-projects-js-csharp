# Assignment 5 — Next.js + React + TalTech ToDo API

A Next.js 16 / React 19 client for the TalTech `ToDo*` backend
(<https://taltech.akaver.com/swagger/index.html>). Covers JWT login/register
with httpOnly cookies and silent refresh, plus full CRUD over tasks,
categories, and priorities. Cross-cutting state is managed with React
Context + `useReducer`; mutations use `useOptimistic` on top of server
actions.

## Public URL

<https://mvoitk-react.proxy.itcollege.ee>

(Proxied to `192.168.181.91:74` on the VPS.)

## Stack

- Next.js **16.2.6** (App Router; in Next 16 middleware is renamed to
  **Proxy** — see [`src/proxy.ts`](src/proxy.ts)).
- React **19.2.4** (`useActionState`, `useOptimistic`, `use()`).
- TypeScript strict mode, `zod` for schema validation.

## Environment variables

| Name               | Default                       | Notes                          |
| ------------------ | ----------------------------- | ------------------------------ |
| `BACKEND_BASE_URL` | `https://taltech.akaver.com`  | Server-only; not exposed to JS |
| `NODE_ENV`         | `development` / `production`  | Controls `Secure` cookie flag  |

Tokens are stored in `httpOnly`, `SameSite=Lax` cookies (`at`, `rt`). In
production they are also marked `Secure`, so the app must be served over
HTTPS end-to-end — the reverse proxy in front of it provides that.

## Local development

```bash
npm install
npm run dev
# http://localhost:3000
```

Other scripts:

- `npm run build` — production build (uses `output: "standalone"`).
- `npm run start` — runs the built app.
- `npm run lint` — ESLint.

## Docker

Build and run locally:

```bash
docker compose up --build
# http://localhost:74
```

Image only:

```bash
docker build -t mvoitk-react:latest .
docker run --rm -p 3000:3000 -e NODE_ENV=production mvoitk-react:latest
```

The Dockerfile is multi-stage (`deps` → `build` → `runner`) and relies on
Next's standalone output, so the final image only ships `server.js`,
`.next/static`, and `public/`. It runs as a non-root user on port 3000.

## VPS deployment

The container listens on `3000` inside Docker and is published on `74` on
the host. The existing IT College reverse proxy
(`mvoitk-react.proxy.itcollege.ee`) terminates TLS and forwards to
`192.168.181.91:74`.

Deploy steps on the VPS:

```bash
git pull
docker compose up -d --build
```

## Smoke checklist

- [x] `npm run build` succeeds, no type errors.
- [x] `npm run lint` clean.
- [ ] Login → todos → create → edit → delete → logout end-to-end.
- [ ] Deleting the `at` cookie triggers a silent refresh on the next request.
- [ ] Deleting both cookies bounces to `/login`.
- [ ] Hitting `/todos` unauthenticated redirects to `/login`.
- [ ] Docker image runs locally on port 74 and behaves the same.
