"use client";

import { createContext, useContext, useMemo, useReducer, type ReactNode } from "react";
import type { TodoCategory, TodoPriority, TodoTask } from "@/lib/api/contracts";
import {
  defaultFilter,
  selectVisible,
  todosReducer,
  type TodosAction,
  type TodosState,
} from "./todosReducer";

interface TodosContextValue {
  state: TodosState;
  visible: TodoTask[];
  dispatch: (action: TodosAction) => void;
}

const TodosContext = createContext<TodosContextValue | null>(null);

export function TodosProvider({
  initialTasks,
  categories,
  priorities,
  children,
}: {
  initialTasks: TodoTask[];
  categories: TodoCategory[];
  priorities: TodoPriority[];
  children: ReactNode;
}) {
  const [state, dispatch] = useReducer(todosReducer, {
    items: initialTasks,
    categories,
    priorities,
    filter: defaultFilter,
    sort: "taskSort",
  });

  const visible = useMemo(() => selectVisible(state), [state]);

  return (
    <TodosContext.Provider value={{ state, visible, dispatch }}>
      {children}
    </TodosContext.Provider>
  );
}

export function useTodos(): TodosContextValue {
  const ctx = useContext(TodosContext);
  if (!ctx) throw new Error("useTodos must be used within a TodosProvider");
  return ctx;
}
