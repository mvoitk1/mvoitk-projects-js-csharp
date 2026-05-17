import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";

export interface TodoTaskRow {
  id: string;
  taskName: string | null;
  taskSort: number;
  createdDt: string;
  dueDt: string | null;
  isCompleted: number;
  isArchived: number;
  todoCategoryId: string | null;
  todoPriorityId: string | null;
  syncDt: string;
}

const COLS =
  "id, taskName, taskSort, createdDt, dueDt, isCompleted, isArchived, todoCategoryId, todoPriorityId, syncDt";

export interface TodoTaskInput {
  taskName?: string | null;
  taskSort?: number;
  createdDt?: string;
  dueDt?: string | null;
  isCompleted?: boolean;
  isArchived?: boolean;
  todoCategoryId?: string | null;
  todoPriorityId?: string | null;
}

export const todoTaskModel = {
  findAllByUser(userId: string): TodoTaskRow[] {
    return db
      .prepare(
        `SELECT ${COLS} FROM todo_tasks
         WHERE userId = ? ORDER BY taskSort, createdDt`,
      )
      .all(userId) as TodoTaskRow[];
  },

  findByIdForUser(userId: string, id: string): TodoTaskRow | undefined {
    return db
      .prepare(`SELECT ${COLS} FROM todo_tasks WHERE userId = ? AND id = ?`)
      .get(userId, id) as TodoTaskRow | undefined;
  },

  create(userId: string, input: TodoTaskInput): TodoTaskRow {
    const id = randomUUID();
    const createdDt = input.createdDt ?? now();
    const syncDt = now();
    db.prepare(
      `INSERT INTO todo_tasks
         (id, userId, taskName, taskSort, createdDt, dueDt, isCompleted, isArchived,
          todoCategoryId, todoPriorityId, syncDt)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    ).run(
      id,
      userId,
      input.taskName ?? null,
      input.taskSort ?? 0,
      createdDt,
      input.dueDt ?? null,
      input.isCompleted ? 1 : 0,
      input.isArchived ? 1 : 0,
      input.todoCategoryId ?? null,
      input.todoPriorityId ?? null,
      syncDt,
    );
    return this.findByIdForUser(userId, id)!;
  },

  update(userId: string, id: string, input: TodoTaskInput): TodoTaskRow | undefined {
    const existing = this.findByIdForUser(userId, id);
    if (!existing) return undefined;
    const syncDt = now();
    db.prepare(
      `UPDATE todo_tasks SET
         taskName = ?, taskSort = ?, createdDt = ?, dueDt = ?,
         isCompleted = ?, isArchived = ?,
         todoCategoryId = ?, todoPriorityId = ?, syncDt = ?
       WHERE userId = ? AND id = ?`,
    ).run(
      input.taskName ?? null,
      input.taskSort ?? 0,
      input.createdDt ?? existing.createdDt,
      input.dueDt ?? null,
      input.isCompleted ? 1 : 0,
      input.isArchived ? 1 : 0,
      input.todoCategoryId ?? null,
      input.todoPriorityId ?? null,
      syncDt,
      userId,
      id,
    );
    return this.findByIdForUser(userId, id);
  },

  remove(userId: string, id: string): boolean {
    return (
      db.prepare(`DELETE FROM todo_tasks WHERE userId = ? AND id = ?`).run(userId, id).changes > 0
    );
  },
};
