import type { TodoCategoryRow } from "../models/todoCategory.js";

export interface TodoCategoryDto {
  id: string;
  categoryName: string | null;
  categorySort: number;
  tag: string | null;
  syncDt: string;
}

export function categoryView(row: TodoCategoryRow): TodoCategoryDto {
  return {
    id: row.id,
    categoryName: row.categoryName,
    categorySort: row.categorySort,
    tag: row.tag,
    syncDt: row.syncDt,
  };
}
