import { Router, type Request, type Response } from "express";
import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";
import { requireAuth } from "../auth.js";

const router = Router();
router.use(requireAuth);

interface TaskRow {
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

interface CategoryRow {
  id: string;
  categoryName: string | null;
  categorySort: number;
  tag: string | null;
  syncDt: string;
}

interface PriorityRow {
  id: string;
  priorityName: string | null;
  prioritySort: number;
  syncDt: string;
}

function hydrate(row: TaskRow, userId: string) {
  const category = row.todoCategoryId
    ? (db
        .prepare(
          `SELECT id, categoryName, categorySort, tag, syncDt
           FROM todo_categories WHERE userId = ? AND id = ?`,
        )
        .get(userId, row.todoCategoryId) as CategoryRow | undefined) ?? null
    : null;
  const priority = row.todoPriorityId
    ? (db
        .prepare(
          `SELECT id, priorityName, prioritySort, syncDt
           FROM todo_priorities WHERE userId = ? AND id = ?`,
        )
        .get(userId, row.todoPriorityId) as PriorityRow | undefined) ?? null
    : null;
  return {
    id: row.id,
    taskName: row.taskName,
    taskSort: row.taskSort,
    createdDt: row.createdDt,
    dueDt: row.dueDt,
    isCompleted: row.isCompleted === 1,
    isArchived: row.isArchived === 1,
    todoCategoryId: row.todoCategoryId,
    todoPriorityId: row.todoPriorityId,
    syncDt: row.syncDt,
    todoCategory: category,
    todoPriority: priority,
  };
}

router.get("/", (req: Request, res: Response) => {
  const rows = db
    .prepare(
      `SELECT id, taskName, taskSort, createdDt, dueDt, isCompleted, isArchived,
              todoCategoryId, todoPriorityId, syncDt
       FROM todo_tasks WHERE userId = ?
       ORDER BY taskSort, createdDt`,
    )
    .all(req.userId) as TaskRow[];
  res.json(rows.map((r) => hydrate(r, req.userId!)));
});

router.get("/:id", (req: Request, res: Response) => {
  const row = db
    .prepare(
      `SELECT id, taskName, taskSort, createdDt, dueDt, isCompleted, isArchived,
              todoCategoryId, todoPriorityId, syncDt
       FROM todo_tasks WHERE userId = ? AND id = ?`,
    )
    .get(req.userId, req.params.id) as TaskRow | undefined;
  if (!row) {
    res.status(404).end();
    return;
  }
  res.json(hydrate(row, req.userId!));
});

interface TaskInput {
  taskName?: string | null;
  taskSort?: number;
  createdDt?: string;
  dueDt?: string | null;
  isCompleted?: boolean;
  isArchived?: boolean;
  todoCategoryId?: string | null;
  todoPriorityId?: string | null;
}

router.post("/", (req: Request, res: Response) => {
  const body = (req.body ?? {}) as TaskInput;
  const id = randomUUID();
  const createdDt = body.createdDt ?? now();
  const syncDt = now();
  db.prepare(
    `INSERT INTO todo_tasks
       (id, userId, taskName, taskSort, createdDt, dueDt, isCompleted, isArchived,
        todoCategoryId, todoPriorityId, syncDt)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
  ).run(
    id,
    req.userId,
    body.taskName ?? null,
    body.taskSort ?? 0,
    createdDt,
    body.dueDt ?? null,
    body.isCompleted ? 1 : 0,
    body.isArchived ? 1 : 0,
    body.todoCategoryId ?? null,
    body.todoPriorityId ?? null,
    syncDt,
  );
  const row = db
    .prepare(
      `SELECT id, taskName, taskSort, createdDt, dueDt, isCompleted, isArchived,
              todoCategoryId, todoPriorityId, syncDt
       FROM todo_tasks WHERE id = ?`,
    )
    .get(id) as TaskRow;
  res.status(201).json(hydrate(row, req.userId!));
});

router.put("/:id", (req: Request, res: Response) => {
  const existing = db
    .prepare(`SELECT createdDt FROM todo_tasks WHERE userId = ? AND id = ?`)
    .get(req.userId, req.params.id) as { createdDt: string } | undefined;
  if (!existing) {
    res.status(404).end();
    return;
  }
  const body = (req.body ?? {}) as TaskInput;
  const syncDt = now();
  db.prepare(
    `UPDATE todo_tasks SET
       taskName = ?, taskSort = ?, createdDt = ?, dueDt = ?,
       isCompleted = ?, isArchived = ?,
       todoCategoryId = ?, todoPriorityId = ?, syncDt = ?
     WHERE userId = ? AND id = ?`,
  ).run(
    body.taskName ?? null,
    body.taskSort ?? 0,
    body.createdDt ?? existing.createdDt,
    body.dueDt ?? null,
    body.isCompleted ? 1 : 0,
    body.isArchived ? 1 : 0,
    body.todoCategoryId ?? null,
    body.todoPriorityId ?? null,
    syncDt,
    req.userId,
    req.params.id,
  );
  const row = db
    .prepare(
      `SELECT id, taskName, taskSort, createdDt, dueDt, isCompleted, isArchived,
              todoCategoryId, todoPriorityId, syncDt
       FROM todo_tasks WHERE id = ?`,
    )
    .get(req.params.id) as TaskRow;
  res.json(hydrate(row, req.userId!));
});

router.delete("/:id", (req: Request, res: Response) => {
  const info = db
    .prepare(`DELETE FROM todo_tasks WHERE userId = ? AND id = ?`)
    .run(req.userId, req.params.id);
  if (info.changes === 0) {
    res.status(404).end();
    return;
  }
  res.status(204).end();
});

export default router;
