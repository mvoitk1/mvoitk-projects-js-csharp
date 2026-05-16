import type { TodoCategory, TodoPriority, TodoTask } from "@/lib/api/contracts";

export type CompletedFilter = "all" | "open" | "completed";
export type SortKey = "taskSort" | "dueDt" | "createdDt";

export interface TodosFilter {
  categoryId: string | null;
  priorityId: string | null;
  completed: CompletedFilter;
  includeArchived: boolean;
}

export interface TodosState {
  items: TodoTask[];
  categories: TodoCategory[];
  priorities: TodoPriority[];
  filter: TodosFilter;
  sort: SortKey;
}

export type TodosAction =
  | { type: "SET"; items: TodoTask[] }
  | { type: "ADD"; item: TodoTask }
  | { type: "UPDATE"; item: TodoTask }
  | { type: "DELETE"; id: string }
  | { type: "SET_FILTER"; filter: Partial<TodosFilter> }
  | { type: "SET_SORT"; sort: SortKey };

export const defaultFilter: TodosFilter = {
  categoryId: null,
  priorityId: null,
  completed: "all",
  includeArchived: false,
};

export function todosReducer(state: TodosState, action: TodosAction): TodosState {
  switch (action.type) {
    case "SET":
      return { ...state, items: action.items };
    case "ADD":
      return { ...state, items: [...state.items, action.item] };
    case "UPDATE":
      return {
        ...state,
        items: state.items.map((t) => (t.id === action.item.id ? action.item : t)),
      };
    case "DELETE":
      return { ...state, items: state.items.filter((t) => t.id !== action.id) };
    case "SET_FILTER":
      return { ...state, filter: { ...state.filter, ...action.filter } };
    case "SET_SORT":
      return { ...state, sort: action.sort };
    default:
      return state;
  }
}

export function selectVisible(state: TodosState): TodoTask[] {
  return selectVisibleItems(state.items, state.filter, state.sort);
}

export function selectVisibleItems(
  items: TodoTask[],
  filter: TodosFilter,
  sort: SortKey,
): TodoTask[] {
  const filtered = items.filter((t) => {
    if (!filter.includeArchived && t.isArchived) return false;
    if (filter.categoryId && t.todoCategoryId !== filter.categoryId) return false;
    if (filter.priorityId && t.todoPriorityId !== filter.priorityId) return false;
    if (filter.completed === "open" && t.isCompleted) return false;
    if (filter.completed === "completed" && !t.isCompleted) return false;
    return true;
  });
  return [...filtered].sort((a, b) => {
    if (sort === "taskSort") return a.taskSort - b.taskSort;
    if (sort === "dueDt") {
      const av = a.dueDt ? Date.parse(a.dueDt) : Number.POSITIVE_INFINITY;
      const bv = b.dueDt ? Date.parse(b.dueDt) : Number.POSITIVE_INFINITY;
      return av - bv;
    }
    return Date.parse(a.createdDt) - Date.parse(b.createdDt);
  });
}
