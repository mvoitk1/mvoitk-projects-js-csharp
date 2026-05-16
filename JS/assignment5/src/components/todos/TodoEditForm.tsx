"use client";

import { useRouter } from "next/navigation";
import { useState, useTransition, type FormEvent } from "react";
import type { TodoCategory, TodoPriority, TodoTask } from "@/lib/api/contracts";
import { deleteTaskAction, updateTaskAction } from "@/app/actions/todos";
import styles from "./TodoEditForm.module.css";

function toDateInput(iso: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toISOString().slice(0, 10);
}

export function TodoEditForm({
  task,
  categories,
  priorities,
}: {
  task: TodoTask;
  categories: TodoCategory[];
  priorities: TodoPriority[];
}) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const data = new FormData(e.currentTarget);
    const taskName = String(data.get("taskName") ?? "").trim();
    const todoCategoryId = String(data.get("todoCategoryId") ?? "");
    const todoPriorityId = String(data.get("todoPriorityId") ?? "");
    const dueRaw = String(data.get("dueDt") ?? "");
    const isCompleted = data.get("isCompleted") === "on";
    const isArchived = data.get("isArchived") === "on";

    if (!taskName || !todoCategoryId || !todoPriorityId) {
      setError("Name, category and priority are required");
      return;
    }
    setError(null);

    startTransition(async () => {
      const res = await updateTaskAction(task.id, {
        ...task,
        taskName,
        todoCategoryId,
        todoPriorityId,
        dueDt: dueRaw ? new Date(dueRaw).toISOString() : null,
        isCompleted,
        isArchived,
      });
      if (!res.ok) {
        setError(res.error);
        return;
      }
      router.push("/todos");
      router.refresh();
    });
  }

  function onDelete() {
    if (!confirm("Delete this task?")) return;
    startTransition(async () => {
      const res = await deleteTaskAction(task.id);
      if (!res.ok) {
        setError(res.error);
        return;
      }
      router.push("/todos");
      router.refresh();
    });
  }

  return (
    <form className={styles.wrap} onSubmit={onSubmit}>
      <div className={styles.field}>
        <label htmlFor="taskName">Name</label>
        <input
          id="taskName"
          name="taskName"
          type="text"
          defaultValue={task.taskName ?? ""}
          required
        />
      </div>

      <div className={styles.field}>
        <label htmlFor="todoCategoryId">Category</label>
        <select
          id="todoCategoryId"
          name="todoCategoryId"
          defaultValue={task.todoCategoryId}
          required
        >
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.categoryName ?? "(unnamed)"}
            </option>
          ))}
        </select>
      </div>

      <div className={styles.field}>
        <label htmlFor="todoPriorityId">Priority</label>
        <select
          id="todoPriorityId"
          name="todoPriorityId"
          defaultValue={task.todoPriorityId}
          required
        >
          {priorities.map((p) => (
            <option key={p.id} value={p.id}>
              {p.priorityName ?? "(unnamed)"}
            </option>
          ))}
        </select>
      </div>

      <div className={styles.field}>
        <label htmlFor="dueDt">Due date</label>
        <input
          id="dueDt"
          name="dueDt"
          type="date"
          defaultValue={toDateInput(task.dueDt)}
        />
      </div>

      <div className={styles.row}>
        <label>
          <input
            name="isCompleted"
            type="checkbox"
            defaultChecked={task.isCompleted}
          />{" "}
          Completed
        </label>
        <label>
          <input
            name="isArchived"
            type="checkbox"
            defaultChecked={task.isArchived}
          />{" "}
          Archived
        </label>
      </div>

      {error && <p className={styles.error}>{error}</p>}

      <div className={styles.actions}>
        <button type="submit" className={styles.button} disabled={pending}>
          Save
        </button>
        <button
          type="button"
          className={`${styles.button} ${styles.delete}`}
          onClick={onDelete}
          disabled={pending}
        >
          Delete
        </button>
      </div>
    </form>
  );
}
