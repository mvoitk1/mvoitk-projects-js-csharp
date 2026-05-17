import type { Request, Response } from "express";
import { todoPriorityModel, type TodoPriorityInput } from "../models/todoPriority.js";
import { priorityView } from "../views/todoPriority.js";

export const prioritiesController = {
  list(req: Request, res: Response): void {
    res.json(todoPriorityModel.findAllByUser(req.userId!).map(priorityView));
  },

  get(req: Request, res: Response): void {
    const row = todoPriorityModel.findByIdForUser(req.userId!, String(req.params.id));
    if (!row) {
      res.status(404).end();
      return;
    }
    res.json(priorityView(row));
  },

  create(req: Request, res: Response): void {
    const row = todoPriorityModel.create(req.userId!, (req.body ?? {}) as TodoPriorityInput);
    res.status(201).json(priorityView(row));
  },

  update(req: Request, res: Response): void {
    const row = todoPriorityModel.update(
      req.userId!,
      String(req.params.id),
      (req.body ?? {}) as TodoPriorityInput,
    );
    if (!row) {
      res.status(404).end();
      return;
    }
    res.json(priorityView(row));
  },

  remove(req: Request, res: Response): void {
    if (!todoPriorityModel.remove(req.userId!, String(req.params.id))) {
      res.status(404).end();
      return;
    }
    res.status(204).end();
  },
};
