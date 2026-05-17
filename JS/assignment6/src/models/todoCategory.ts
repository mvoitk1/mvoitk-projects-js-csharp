import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";

export interface TodoCategoryRow {
  id: string;
  categoryName: string | null;
  categorySort: number;
  tag: string | null;
  syncDt: string;
}

const COLS = "id, categoryName, categorySort, tag, syncDt";

export interface TodoCategoryInput {
  categoryName?: string | null;
  categorySort?: number;
  tag?: string | null;
}

export const todoCategoryModel = {
  findAllByUser(userId: string): TodoCategoryRow[] {
    return db
      .prepare(
        `SELECT ${COLS} FROM todo_categories
         WHERE userId = ? ORDER BY categorySort, categoryName`,
      )
      .all(userId) as TodoCategoryRow[];
  },

  findByIdForUser(userId: string, id: string): TodoCategoryRow | undefined {
    return db
      .prepare(`SELECT ${COLS} FROM todo_categories WHERE userId = ? AND id = ?`)
      .get(userId, id) as TodoCategoryRow | undefined;
  },

  create(userId: string, input: TodoCategoryInput): TodoCategoryRow {
    const row: TodoCategoryRow = {
      id: randomUUID(),
      categoryName: input.categoryName ?? null,
      categorySort: input.categorySort ?? 0,
      tag: input.tag ?? null,
      syncDt: now(),
    };
    db.prepare(
      `INSERT INTO todo_categories (id, userId, categoryName, categorySort, tag, syncDt)
       VALUES (?, ?, ?, ?, ?, ?)`,
    ).run(row.id, userId, row.categoryName, row.categorySort, row.tag, row.syncDt);
    return row;
  },

  update(userId: string, id: string, input: TodoCategoryInput): TodoCategoryRow | undefined {
    if (!this.findByIdForUser(userId, id)) return undefined;
    const row: TodoCategoryRow = {
      id,
      categoryName: input.categoryName ?? null,
      categorySort: input.categorySort ?? 0,
      tag: input.tag ?? null,
      syncDt: now(),
    };
    db.prepare(
      `UPDATE todo_categories
       SET categoryName = ?, categorySort = ?, tag = ?, syncDt = ?
       WHERE userId = ? AND id = ?`,
    ).run(row.categoryName, row.categorySort, row.tag, row.syncDt, userId, id);
    return row;
  },

  remove(userId: string, id: string): boolean {
    return (
      db.prepare(`DELETE FROM todo_categories WHERE userId = ? AND id = ?`).run(userId, id)
        .changes > 0
    );
  },
};
