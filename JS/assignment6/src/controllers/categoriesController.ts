import type { Request, Response } from "express";
import { todoCategoryModel, type TodoCategoryInput } from "../models/todoCategory.js";
import { categoryView } from "../views/todoCategory.js";

export const categoriesController = {
  list(req: Request, res: Response): void {
    res.json(todoCategoryModel.findAllByUser(req.userId!).map(categoryView));
  },

  get(req: Request, res: Response): void {
    const row = todoCategoryModel.findByIdForUser(req.userId!, String(req.params.id));
    if (!row) {
      res.status(404).end();
      return;
    }
    res.json(categoryView(row));
  },

  create(req: Request, res: Response): void {
    const row = todoCategoryModel.create(req.userId!, (req.body ?? {}) as TodoCategoryInput);
    res.status(201).json(categoryView(row));
  },

  update(req: Request, res: Response): void {
    const row = todoCategoryModel.update(
      req.userId!,
      String(req.params.id),
      (req.body ?? {}) as TodoCategoryInput,
    );
    if (!row) {
      res.status(404).end();
      return;
    }
    res.json(categoryView(row));
  },

  remove(req: Request, res: Response): void {
    if (!todoCategoryModel.remove(req.userId!, String(req.params.id))) {
      res.status(404).end();
      return;
    }
    res.status(204).end();
  },
};
