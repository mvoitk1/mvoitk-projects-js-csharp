https://mvoitk-js2.proxy.itcollege.ee/

# Task Forge

Task Forge is a browser-based task manager built with TypeScript and Vite. It provides a dashboard, task list, calendar view, and modal-based task editing, with data persisted in `localStorage`.

## Features

- Create, edit, and delete tasks
- Track task status and priority
- Add due dates, tags, and checklist items
- Search tasks from the top bar
- View summary stats on the dashboard
- Browse upcoming work in a calendar view
- Persist data locally in the browser with `localStorage`

## Tech Stack

- TypeScript
- Vite
- Vitest
- Browser `localStorage`
- Docker + Nginx for containerized serving

## Getting Started

### Prerequisites

- Node.js 20+ recommended
- npm

### Install dependencies

```bash
npm install
```

### Start the development server

```bash
npm run dev
```

### Build for production

```bash
npm run build
```

### Preview the production build

```bash
npm run preview
```

### Run tests

```bash
npm test
```

## Docker

Build and run with Docker Compose:

```bash
docker compose up --build
```

The app will be available at `http://localhost:78`.

## Project Structure

```text
.
├── src/
│   ├── js/
│   │   ├── app.ts
│   │   ├── dataAdapter.ts
│   │   ├── storage.ts
│   │   ├── taskManager.ts
│   │   └── ...
│   ├── styles/
│   │   └── app.css
│   └── main.ts
├── index.html
├── Dockerfile
├── docker-compose.yml
├── package.json
└── vitest.config.ts
```

## Notes

- Task data is stored in the browser, so clearing site storage will remove saved tasks.
- The production build uses `vite build --base=./` so the generated app can be served from a static container.
