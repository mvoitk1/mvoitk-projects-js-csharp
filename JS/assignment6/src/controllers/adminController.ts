import type { Request, Response } from "express";
import { adminModel } from "../models/admin.js";

function ownerLabel(
  email: string,
  firstName: string | null,
  lastName: string | null,
): string {
  const name = [firstName, lastName].filter(Boolean).join(" ").trim();
  return name ? `${name} <${email}>` : email;
}

export const adminController = {
  overview(_req: Request, res: Response): void {
    const tasks = adminModel.allTasks().map((t) => ({
      ...t,
      owner: ownerLabel(t.ownerEmail, t.ownerFirstName, t.ownerLastName),
      isCompleted: t.isCompleted === 1,
      isArchived: t.isArchived === 1,
    }));
    const categories = adminModel.allCategories().map((c) => ({
      ...c,
      owner: ownerLabel(c.ownerEmail, c.ownerFirstName, c.ownerLastName),
    }));
    const priorities = adminModel.allPriorities().map((p) => ({
      ...p,
      owner: ownerLabel(p.ownerEmail, p.ownerFirstName, p.ownerLastName),
    }));
    res.render("overview", { tasks, categories, priorities });
  },
};
