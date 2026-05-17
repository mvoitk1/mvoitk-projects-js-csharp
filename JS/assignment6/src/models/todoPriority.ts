import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";

export interface TodoPriorityRow {
  id: string;
  priorityName: string | null;
  prioritySort: number;
  syncDt: string;
}

const COLS = "id, priorityName, prioritySort, syncDt";

export interface TodoPriorityInput {
  priorityName?: string | null;
  prioritySort?: number;
}

export const todoPriorityModel = {
  findAllByUser(userId: string): TodoPriorityRow[] {
    return db
      .prepare(
        `SELECT ${COLS} FROM todo_priorities
         WHERE userId = ? ORDER BY prioritySort, priorityName`,
      )
      .all(userId) as TodoPriorityRow[];
  },

  findByIdForUser(userId: string, id: string): TodoPriorityRow | undefined {
    return db
      .prepare(`SELECT ${COLS} FROM todo_priorities WHERE userId = ? AND id = ?`)
      .get(userId, id) as TodoPriorityRow | undefined;
  },

  create(userId: string, input: TodoPriorityInput): TodoPriorityRow {
    const row: TodoPriorityRow = {
      id: randomUUID(),
      priorityName: input.priorityName ?? null,
      prioritySort: input.prioritySort ?? 0,
      syncDt: now(),
    };
    db.prepare(
      `INSERT INTO todo_priorities (id, userId, priorityName, prioritySort, syncDt)
       VALUES (?, ?, ?, ?, ?)`,
    ).run(row.id, userId, row.priorityName, row.prioritySort, row.syncDt);
    return row;
  },

  update(userId: string, id: string, input: TodoPriorityInput): TodoPriorityRow | undefined {
    if (!this.findByIdForUser(userId, id)) return undefined;
    const row: TodoPriorityRow = {
      id,
      priorityName: input.priorityName ?? null,
      prioritySort: input.prioritySort ?? 0,
      syncDt: now(),
    };
    db.prepare(
      `UPDATE todo_priorities
       SET priorityName = ?, prioritySort = ?, syncDt = ?
       WHERE userId = ? AND id = ?`,
    ).run(row.priorityName, row.prioritySort, row.syncDt, userId, id);
    return row;
  },

  remove(userId: string, id: string): boolean {
    return (
      db.prepare(`DELETE FROM todo_priorities WHERE userId = ? AND id = ?`).run(userId, id)
        .changes > 0
    );
  },
};
