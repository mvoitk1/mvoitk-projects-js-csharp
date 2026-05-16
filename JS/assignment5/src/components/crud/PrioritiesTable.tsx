"use client";

import { useState, useTransition, type FormEvent } from "react";
import type { TodoPriority } from "@/lib/api/contracts";
import {
  createPriorityAction,
  deletePriorityAction,
  updatePriorityAction,
} from "@/app/actions/priorities";
import styles from "./CrudTable.module.css";

type RowDraft = {
  priorityName: string;
  prioritySort: string;
};

function toDraft(p: TodoPriority): RowDraft {
  return {
    priorityName: p.priorityName ?? "",
    prioritySort: String(p.prioritySort),
  };
}

export function PrioritiesTable({ initial }: { initial: TodoPriority[] }) {
  const [items, setItems] = useState(initial);
  const [drafts, setDrafts] = useState<Record<string, RowDraft>>(() =>
    Object.fromEntries(initial.map((p) => [p.id, toDraft(p)])),
  );
  const [error, setError] = useState<string | null>(null);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [, startTransition] = useTransition();

  function setDraft(id: string, patch: Partial<RowDraft>) {
    setDrafts((prev) => ({ ...prev, [id]: { ...prev[id], ...patch } }));
  }

  function handleCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = e.currentTarget;
    const data = new FormData(form);
    const priorityName = String(data.get("priorityName") ?? "").trim();
    const prioritySort = Number(data.get("prioritySort") ?? 0);
    if (!priorityName) {
      setError("Name is required");
      return;
    }
    setError(null);
    setPendingId("new");
    startTransition(async () => {
      const res = await createPriorityAction({
        priorityName,
        prioritySort: Number.isFinite(prioritySort) ? prioritySort : 0,
        syncDt: new Date().toISOString(),
      });
      setPendingId(null);
      if (!res.ok) {
        setError(res.error);
        return;
      }
      setItems((prev) => [...prev, res.item]);
      setDrafts((prev) => ({ ...prev, [res.item.id]: toDraft(res.item) }));
      form.reset();
    });
  }

  function handleSave(item: TodoPriority) {
    const d = drafts[item.id];
    if (!d.priorityName.trim()) {
      setError("Name is required");
      return;
    }
    setError(null);
    setPendingId(item.id);
    startTransition(async () => {
      const res = await updatePriorityAction({
        ...item,
        priorityName: d.priorityName.trim(),
        prioritySort: Number(d.prioritySort) || 0,
      });
      setPendingId(null);
      if (!res.ok) {
        setError(res.error);
        return;
      }
      setItems((prev) => prev.map((p) => (p.id === res.item.id ? res.item : p)));
      setDrafts((prev) => ({ ...prev, [res.item.id]: toDraft(res.item) }));
    });
  }

  function handleDelete(id: string) {
    if (!confirm("Delete this priority?")) return;
    setError(null);
    setPendingId(id);
    startTransition(async () => {
      const res = await deletePriorityAction(id);
      setPendingId(null);
      if (!res.ok) {
        setError(res.error);
        return;
      }
      setItems((prev) => prev.filter((p) => p.id !== id));
      setDrafts((prev) => {
        const next = { ...prev };
        delete next[id];
        return next;
      });
    });
  }

  return (
    <div className={styles.wrap}>
      <h1 className={styles.heading}>Priorities</h1>

      <form className={styles.wrap} onSubmit={handleCreate} style={{ padding: 0 }}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Sort</th>
              <th />
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <input name="priorityName" type="text" placeholder="New priority" />
              </td>
              <td>
                <input
                  name="prioritySort"
                  type="number"
                  defaultValue={0}
                  style={{ width: "5rem" }}
                />
              </td>
              <td className={styles.actions}>
                <button
                  type="submit"
                  className={styles.button}
                  disabled={pendingId === "new"}
                >
                  Add
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </form>

      {error && <p className={styles.error}>{error}</p>}

      {items.length === 0 ? (
        <p className={styles.empty}>No priorities yet.</p>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Sort</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {items.map((item) => {
              const d = drafts[item.id] ?? toDraft(item);
              const isPending = pendingId === item.id;
              return (
                <tr key={item.id}>
                  <td>
                    <input
                      type="text"
                      value={d.priorityName}
                      onChange={(e) =>
                        setDraft(item.id, { priorityName: e.target.value })
                      }
                      disabled={isPending}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      value={d.prioritySort}
                      onChange={(e) =>
                        setDraft(item.id, { prioritySort: e.target.value })
                      }
                      disabled={isPending}
                      style={{ width: "5rem" }}
                    />
                  </td>
                  <td className={styles.actions}>
                    <button
                      type="button"
                      className={styles.button}
                      onClick={() => handleSave(item)}
                      disabled={isPending}
                    >
                      Save
                    </button>
                    <button
                      type="button"
                      className={`${styles.button} ${styles.delete}`}
                      onClick={() => handleDelete(item.id)}
                      disabled={isPending}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
