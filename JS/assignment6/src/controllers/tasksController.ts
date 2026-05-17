import type { Request, Response } from "express";
import { todoTaskModel, type TodoTaskInput } from "../models/todoTask.js";
import { taskView } from "../views/todoTask.js";

export const tasksController = {
  list(req: Request, res: Response): void {
    const userId = req.userId!;
    res.json(todoTaskModel.findAllByUser(userId).map((r) => taskView(r, userId)));
  },

  get(req: Request, res: Response): void {
    const userId = req.userId!;
    const row = todoTaskModel.findByIdForUser(userId, String(req.params.id));
    if (!row) {
      res.status(404).end();
      return;
    }
    res.json(taskView(row, userId));
  },

  create(req: Request, res: Response): void {
    const userId = req.userId!;
    const row = todoTaskModel.create(userId, (req.body ?? {}) as TodoTaskInput);
    res.status(201).json(taskView(row, userId));
  },

  update(req: Request, res: Response): void {
    const userId = req.userId!;
    const row = todoTaskModel.update(userId, String(req.params.id), (req.body ?? {}) as TodoTaskInput);
    if (!row) {
      res.status(404).end();
      return;
    }
    res.json(taskView(row, userId));
  },

  remove(req: Request, res: Response): void {
    if (!todoTaskModel.remove(req.userId!, String(req.params.id))) {
      res.status(404).end();
      return;
    }
    res.status(204).end();
  },
};
