import type { TodoTaskRow } from "../models/todoTask.js";
import { todoCategoryModel } from "../models/todoCategory.js";
import { todoPriorityModel } from "../models/todoPriority.js";
import { categoryView, type TodoCategoryDto } from "./todoCategory.js";
import { priorityView, type TodoPriorityDto } from "./todoPriority.js";

export interface TodoTaskDto {
  id: string;
  taskName: string | null;
  taskSort: number;
  createdDt: string;
  dueDt: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: string | null;
  todoPriorityId: string | null;
  syncDt: string;
  todoCategory: TodoCategoryDto | null;
  todoPriority: TodoPriorityDto | null;
}

export function taskView(row: TodoTaskRow, userId: string): TodoTaskDto {
  const cat = row.todoCategoryId
    ? todoCategoryModel.findByIdForUser(userId, row.todoCategoryId)
    : undefined;
  const pri = row.todoPriorityId
    ? todoPriorityModel.findByIdForUser(userId, row.todoPriorityId)
    : undefined;
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
    todoCategory: cat ? categoryView(cat) : null,
    todoPriority: pri ? priorityView(pri) : null,
  };
}
