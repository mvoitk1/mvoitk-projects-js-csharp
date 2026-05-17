import { db } from "../db.js";

export interface TaskWithOwner {
  id: string;
  taskName: string | null;
  taskSort: number;
  createdDt: string;
  dueDt: string | null;
  isCompleted: number;
  isArchived: number;
  syncDt: string;
  ownerEmail: string;
  ownerFirstName: string | null;
  ownerLastName: string | null;
  categoryName: string | null;
  priorityName: string | null;
}

export interface CategoryWithOwner {
  id: string;
  categoryName: string | null;
  categorySort: number;
  tag: string | null;
  syncDt: string;
  ownerEmail: string;
  ownerFirstName: string | null;
  ownerLastName: string | null;
}

export interface PriorityWithOwner {
  id: string;
  priorityName: string | null;
  prioritySort: number;
  syncDt: string;
  ownerEmail: string;
  ownerFirstName: string | null;
  ownerLastName: string | null;
}

export const adminModel = {
  allTasks(): TaskWithOwner[] {
    return db
      .prepare(
        `SELECT t.id, t.taskName, t.taskSort, t.createdDt, t.dueDt,
                t.isCompleted, t.isArchived, t.syncDt,
                u.email     AS ownerEmail,
                u.firstName AS ownerFirstName,
                u.lastName  AS ownerLastName,
                c.categoryName AS categoryName,
                p.priorityName AS priorityName
         FROM todo_tasks t
         JOIN users u ON u.id = t.userId
         LEFT JOIN todo_categories c ON c.id = t.todoCategoryId
         LEFT JOIN todo_priorities p ON p.id = t.todoPriorityId
         ORDER BY t.createdDt DESC`,
      )
      .all() as TaskWithOwner[];
  },

  allCategories(): CategoryWithOwner[] {
    return db
      .prepare(
        `SELECT c.id, c.categoryName, c.categorySort, c.tag, c.syncDt,
                u.email     AS ownerEmail,
                u.firstName AS ownerFirstName,
                u.lastName  AS ownerLastName
         FROM todo_categories c
         JOIN users u ON u.id = c.userId
         ORDER BY u.email, c.categorySort`,
      )
      .all() as CategoryWithOwner[];
  },

  allPriorities(): PriorityWithOwner[] {
    return db
      .prepare(
        `SELECT p.id, p.priorityName, p.prioritySort, p.syncDt,
                u.email     AS ownerEmail,
                u.firstName AS ownerFirstName,
                u.lastName  AS ownerLastName
         FROM todo_priorities p
         JOIN users u ON u.id = p.userId
         ORDER BY u.email, p.prioritySort`,
      )
      .all() as PriorityWithOwner[];
  },
};
