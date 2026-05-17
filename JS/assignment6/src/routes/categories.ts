import { Router, type Request, type Response } from "express";
import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";
import { requireAuth } from "../auth.js";

const router = Router();
router.use(requireAuth);

interface CategoryRow {
  id: string;
  categoryName: string | null;
  categorySort: number;
  tag: string | null;
  syncDt: string;
}

router.get("/", (req: Request, res: Response) => {
  const rows = db
    .prepare(
      `SELECT id, categoryName, categorySort, tag, syncDt
       FROM todo_categories WHERE userId = ? ORDER BY categorySort, categoryName`,
    )
    .all(req.userId);
  res.json(rows);
});

router.get("/:id", (req: Request, res: Response) => {
  const row = db
    .prepare(
      `SELECT id, categoryName, categorySort, tag, syncDt
       FROM todo_categories WHERE userId = ? AND id = ?`,
    )
    .get(req.userId, req.params.id);
  if (!row) {
    res.status(404).end();
    return;
  }
  res.json(row);
});

router.post("/", (req: Request, res: Response) => {
  const { categoryName, categorySort, tag } = (req.body ?? {}) as Partial<CategoryRow>;
  const id = randomUUID();
  const syncDt = now();
  db.prepare(
    `INSERT INTO todo_categories (id, userId, categoryName, categorySort, tag, syncDt)
     VALUES (?, ?, ?, ?, ?, ?)`,
  ).run(id, req.userId, categoryName ?? null, categorySort ?? 0, tag ?? null, syncDt);
  res.status(201).json({
    id,
    categoryName: categoryName ?? null,
    categorySort: categorySort ?? 0,
    tag: tag ?? null,
    syncDt,
  });
});

router.put("/:id", (req: Request, res: Response) => {
  const existing = db
    .prepare(`SELECT id FROM todo_categories WHERE userId = ? AND id = ?`)
    .get(req.userId, req.params.id);
  if (!existing) {
    res.status(404).end();
    return;
  }
  const { categoryName, categorySort, tag } = (req.body ?? {}) as Partial<CategoryRow>;
  const syncDt = now();
  db.prepare(
    `UPDATE todo_categories
     SET categoryName = ?, categorySort = ?, tag = ?, syncDt = ?
     WHERE userId = ? AND id = ?`,
  ).run(
    categoryName ?? null,
    categorySort ?? 0,
    tag ?? null,
    syncDt,
    req.userId,
    req.params.id,
  );
  res.json({
    id: req.params.id,
    categoryName: categoryName ?? null,
    categorySort: categorySort ?? 0,
    tag: tag ?? null,
    syncDt,
  });
});

router.delete("/:id", (req: Request, res: Response) => {
  const info = db
    .prepare(`DELETE FROM todo_categories WHERE userId = ? AND id = ?`)
    .run(req.userId, req.params.id);
  if (info.changes === 0) {
    res.status(404).end();
    return;
  }
  res.status(204).end();
});

export default router;
