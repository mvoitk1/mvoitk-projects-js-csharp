"use client";

import Link from "next/link";
import {
  startTransition,
  useOptimistic,
  useState,
  type FormEvent,
} from "react";
import type { TodoTask } from "@/lib/api/contracts";
import {
  createTaskAction,
  deleteTaskAction,
  updateTaskAction,
} from "@/app/actions/todos";
import { useTodos } from "@/state/todos/TodosContext";
import {
  selectVisibleItems,
  type CompletedFilter,
  type SortKey,
} from "@/state/todos/todosReducer";
import styles from "./TodoList.module.css";

type Optimistic =
  | { type: "add"; item: TodoTask }
  | { type: "update"; item: TodoTask }
  | { type: "delete"; id: string };

function applyOptimistic(items: TodoTask[], action: Optimistic): TodoTask[] {
  switch (action.type) {
    case "add":
      return [...items, action.item];
    case "update":
      return items.map((t) => (t.id === action.item.id ? action.item : t));
    case "delete":
      return items.filter((t) => t.id !== action.id);
  }
}

function formatDate(iso: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString();
}

function tempId(): string {
  return `temp-${Math.random().toString(36).slice(2)}`;
}

export function TodoList() {
  const { state, dispatch } = useTodos();
  const { categories, priorities, filter, sort } = state;
  const [optimisticItems, addOptimistic] = useOptimistic<TodoTask[], Optimistic>(
    state.items,
    applyOptimistic,
  );
  const [error, setError] = useState<string | null>(null);

  const visible = selectVisibleItems(optimisticItems, filter, sort);
  const pendingIds = new Set(
    optimisticItems
      .map((t) => t.id)
      .filter((id) => !state.items.some((s) => s.id === id)),
  );

  const categoryName = (id: string) =>
    categories.find((c) => c.id === id)?.categoryName ?? "—";
  const priorityName = (id: string) =>
    priorities.find((p) => p.id === id)?.priorityName ?? "—";

  function handleCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = e.currentTarget;
    const data = new FormData(form);
    const taskName = String(data.get("taskName") ?? "").trim();
    const todoCategoryId = String(data.get("todoCategoryId") ?? "");
    const todoPriorityId = String(data.get("todoPriorityId") ?? "");
    const dueRaw = String(data.get("dueDt") ?? "");
    if (!taskName || !todoCategoryId || !todoPriorityId) {
      setError("Name, category and priority are required");
      return;
    }
    setError(null);
    const now = new Date().toISOString();
    const optimistic: TodoTask = {
      id: tempId(),
      taskName,
      taskSort: state.items.length,
      createdDt: now,
      dueDt: dueRaw ? new Date(dueRaw).toISOString() : null,
      isCompleted: false,
      isArchived: false,
      todoCategoryId,
      todoPriorityId,
      syncDt: now,
    };
    form.reset();
    startTransition(async () => {
      addOptimistic({ type: "add", item: optimistic });
      const res = await createTaskAction({
        taskName,
        taskSort: optimistic.taskSort,
        createdDt: now,
        dueDt: optimistic.dueDt,
        isCompleted: false,
        isArchived: false,
        todoCategoryId,
        todoPriorityId,
      });
      if (res.ok) dispatch({ type: "ADD", item: res.item });
      else setError(res.error);
    });
  }

  function handleToggle(task: TodoTask) {
    const updated: TodoTask = { ...task, isCompleted: !task.isCompleted };
    startTransition(async () => {
      addOptimistic({ type: "update", item: updated });
      const res = await updateTaskAction(task.id, updated);
      if (res.ok) dispatch({ type: "UPDATE", item: res.item });
      else setError(res.error);
    });
  }

  function handleDelete(id: string) {
    startTransition(async () => {
      addOptimistic({ type: "delete", id });
      const res = await deleteTaskAction(id);
      if (res.ok) dispatch({ type: "DELETE", id });
      else setError(res.error);
    });
  }

  return (
    <div className={styles.wrap}>
      <form className={styles.createForm} onSubmit={handleCreate}>
        <input
          type="text"
          name="taskName"
          placeholder="New task name"
          required
        />
        <select name="todoCategoryId" defaultValue="" required>
          <option value="" disabled>
            Category…
          </option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.categoryName ?? "(unnamed)"}
            </option>
          ))}
        </select>
        <select name="todoPriorityId" defaultValue="" required>
          <option value="" disabled>
            Priority…
          </option>
          {priorities.map((p) => (
            <option key={p.id} value={p.id}>
              {p.priorityName ?? "(unnamed)"}
            </option>
          ))}
        </select>
        <input type="date" name="dueDt" />
        <button type="submit" className={styles.button}>
          Add
        </button>
      </form>

      {error && <p className={styles.error}>{error}</p>}

      <div className={styles.filters}>
        <label>
          Category
          <select
            value={filter.categoryId ?? ""}
            onChange={(e) =>
              dispatch({
                type: "SET_FILTER",
                filter: { categoryId: e.target.value || null },
              })
            }
          >
            <option value="">All</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.categoryName ?? "(unnamed)"}
              </option>
            ))}
          </select>
        </label>

        <label>
          Priority
          <select
            value={filter.priorityId ?? ""}
            onChange={(e) =>
              dispatch({
                type: "SET_FILTER",
                filter: { priorityId: e.target.value || null },
              })
            }
          >
            <option value="">All</option>
            {priorities.map((p) => (
              <option key={p.id} value={p.id}>
                {p.priorityName ?? "(unnamed)"}
              </option>
            ))}
          </select>
        </label>

        <label>
          Status
          <select
            value={filter.completed}
            onChange={(e) =>
              dispatch({
                type: "SET_FILTER",
                filter: { completed: e.target.value as CompletedFilter },
              })
            }
          >
            <option value="all">All</option>
            <option value="open">Open</option>
            <option value="completed">Completed</option>
          </select>
        </label>

        <label>
          <input
            type="checkbox"
            checked={filter.includeArchived}
            onChange={(e) =>
              dispatch({
                type: "SET_FILTER",
                filter: { includeArchived: e.target.checked },
              })
            }
          />
          Include archived
        </label>

        <label>
          Sort
          <select
            value={sort}
            onChange={(e) =>
              dispatch({ type: "SET_SORT", sort: e.target.value as SortKey })
            }
          >
            <option value="taskSort">Manual</option>
            <option value="dueDt">Due date</option>
            <option value="createdDt">Created</option>
          </select>
        </label>
      </div>

      {visible.length === 0 ? (
        <p className={styles.empty}>No tasks match the current filter.</p>
      ) : (
        <ul className={styles.list}>
          {visible.map((t) => {
            const pending = pendingIds.has(t.id);
            return (
              <li
                key={t.id}
                className={styles.item}
                data-completed={t.isCompleted}
                data-archived={t.isArchived}
                data-pending={pending}
              >
                <input
                  type="checkbox"
                  checked={t.isCompleted}
                  onChange={() => handleToggle(t)}
                  disabled={pending}
                  aria-label={`Mark ${t.taskName ?? "task"} ${
                    t.isCompleted ? "incomplete" : "complete"
                  }`}
                />
                <Link href={`/todos/${t.id}`} className={styles.itemLink}>
                  <span className={styles.name}>{t.taskName ?? "(untitled)"}</span>
                </Link>
                <span className={styles.meta}>
                  <span className={styles.tag}>{categoryName(t.todoCategoryId)}</span>
                  <span className={styles.tag}>{priorityName(t.todoPriorityId)}</span>
                  <span>due {formatDate(t.dueDt)}</span>
                </span>
                <button
                  type="button"
                  className={`${styles.button} ${styles.delete}`}
                  onClick={() => handleDelete(t.id)}
                  disabled={pending}
                >
                  Delete
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
