import Database, { type Database as DatabaseType } from "better-sqlite3";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";

const dbPath = resolve(process.env.DB_PATH ?? "data/app.db");
mkdirSync(dirname(dbPath), { recursive: true });

export const db: DatabaseType = new Database(dbPath);
db.pragma("journal_mode = WAL");
db.pragma("foreign_keys = ON");

db.exec(`
  CREATE TABLE IF NOT EXISTS users (
    id           TEXT PRIMARY KEY,
    email        TEXT UNIQUE NOT NULL,
    passwordHash TEXT NOT NULL,
    firstName    TEXT,
    lastName     TEXT,
    createdDt    TEXT NOT NULL
  );

  CREATE TABLE IF NOT EXISTS refresh_tokens (
    id        TEXT PRIMARY KEY,
    userId    TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tokenHash TEXT NOT NULL,
    jwtHash   TEXT NOT NULL,
    expiresAt TEXT NOT NULL,
    createdDt TEXT NOT NULL
  );
  CREATE INDEX IF NOT EXISTS idx_refresh_tokens_userId ON refresh_tokens(userId);

  CREATE TABLE IF NOT EXISTS todo_categories (
    id           TEXT PRIMARY KEY,
    userId       TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    categoryName TEXT,
    categorySort INTEGER NOT NULL DEFAULT 0,
    tag          TEXT,
    syncDt       TEXT NOT NULL
  );
  CREATE INDEX IF NOT EXISTS idx_todo_categories_userId ON todo_categories(userId);

  CREATE TABLE IF NOT EXISTS todo_priorities (
    id           TEXT PRIMARY KEY,
    userId       TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    priorityName TEXT,
    prioritySort INTEGER NOT NULL DEFAULT 0,
    syncDt       TEXT NOT NULL
  );
  CREATE INDEX IF NOT EXISTS idx_todo_priorities_userId ON todo_priorities(userId);

  CREATE TABLE IF NOT EXISTS todo_tasks (
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
  CREATE INDEX IF NOT EXISTS idx_todo_tasks_userId ON todo_tasks(userId);
`);

export function now(): string {
  return new Date().toISOString();
}
