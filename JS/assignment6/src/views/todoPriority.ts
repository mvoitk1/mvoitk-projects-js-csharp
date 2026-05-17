import type { TodoPriorityRow } from "../models/todoPriority.js";

export interface TodoPriorityDto {
  id: string;
  priorityName: string | null;
  prioritySort: number;
  syncDt: string;
}

export function priorityView(row: TodoPriorityRow): TodoPriorityDto {
  return {
    id: row.id,
    priorityName: row.priorityName,
    prioritySort: row.prioritySort,
    syncDt: row.syncDt,
  };
}
