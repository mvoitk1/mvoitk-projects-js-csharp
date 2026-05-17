import { Router, type Request, type Response } from "express";
import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";
import { requireAuth } from "../auth.js";

const router = Router();
router.use(requireAuth);

interface PriorityRow {
  id: string;
  priorityName: string | null;
  prioritySort: number;
  syncDt: string;
}

router.get("/", (req: Request, res: Response) => {
  const rows = db
    .prepare(
      `SELECT id, priorityName, prioritySort, syncDt
       FROM todo_priorities WHERE userId = ? ORDER BY prioritySort, priorityName`,
    )
    .all(req.userId);
  res.json(rows);
});

router.get("/:id", (req: Request, res: Response) => {
  const row = db
    .prepare(
      `SELECT id, priorityName, prioritySort, syncDt
       FROM todo_priorities WHERE userId = ? AND id = ?`,
    )
    .get(req.userId, req.params.id);
  if (!row) {
    res.status(404).end();
    return;
  }
  res.json(row);
});

router.post("/", (req: Request, res: Response) => {
  const { priorityName, prioritySort } = (req.body ?? {}) as Partial<PriorityRow>;
  const id = randomUUID();
  const syncDt = now();
  db.prepare(
    `INSERT INTO todo_priorities (id, userId, priorityName, prioritySort, syncDt)
     VALUES (?, ?, ?, ?, ?)`,
  ).run(id, req.userId, priorityName ?? null, prioritySort ?? 0, syncDt);
  res.status(201).json({
    id,
    priorityName: priorityName ?? null,
    prioritySort: prioritySort ?? 0,
    syncDt,
  });
});

router.put("/:id", (req: Request, res: Response) => {
  const existing = db
    .prepare(`SELECT id FROM todo_priorities WHERE userId = ? AND id = ?`)
    .get(req.userId, req.params.id);
  if (!existing) {
    res.status(404).end();
    return;
  }
  const { priorityName, prioritySort } = (req.body ?? {}) as Partial<PriorityRow>;
  const syncDt = now();
  db.prepare(
    `UPDATE todo_priorities
     SET priorityName = ?, prioritySort = ?, syncDt = ?
     WHERE userId = ? AND id = ?`,
  ).run(priorityName ?? null, prioritySort ?? 0, syncDt, req.userId, req.params.id);
  res.json({
    id: req.params.id,
    priorityName: priorityName ?? null,
    prioritySort: prioritySort ?? 0,
    syncDt,
  });
});

router.delete("/:id", (req: Request, res: Response) => {
  const info = db
    .prepare(`DELETE FROM todo_priorities WHERE userId = ? AND id = ?`)
    .run(req.userId, req.params.id);
  if (info.changes === 0) {
    res.status(404).end();
    return;
  }
  res.status(204).end();
});

export default router;
