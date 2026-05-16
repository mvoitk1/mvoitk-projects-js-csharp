"use client";

import { useState, useTransition, type FormEvent } from "react";
import type { TodoCategory } from "@/lib/api/contracts";
import {
  createCategoryAction,
  deleteCategoryAction,
  updateCategoryAction,
} from "@/app/actions/categories";
import styles from "./CrudTable.module.css";

type RowDraft = {
  categoryName: string;
  categorySort: string;
  tag: string;
};

function toDraft(c: TodoCategory): RowDraft {
  return {
    categoryName: c.categoryName ?? "",
    categorySort: String(c.categorySort),
    tag: c.tag ?? "",
  };
}

export function CategoriesTable({ initial }: { initial: TodoCategory[] }) {
  const [items, setItems] = useState(initial);
  const [drafts, setDrafts] = useState<Record<string, RowDraft>>(() =>
    Object.fromEntries(initial.map((c) => [c.id, toDraft(c)])),
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
    const categoryName = String(data.get("categoryName") ?? "").trim();
    const categorySort = Number(data.get("categorySort") ?? 0);
    const tag = String(data.get("tag") ?? "").trim();
    if (!categoryName) {
      setError("Name is required");
      return;
    }
    setError(null);
    setPendingId("new");
    startTransition(async () => {
      const res = await createCategoryAction({
        categoryName,
        categorySort: Number.isFinite(categorySort) ? categorySort : 0,
        tag: tag || null,
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

  function handleSave(item: TodoCategory) {
    const d = drafts[item.id];
    if (!d.categoryName.trim()) {
      setError("Name is required");
      return;
    }
    setError(null);
    setPendingId(item.id);
    startTransition(async () => {
      const res = await updateCategoryAction({
        ...item,
        categoryName: d.categoryName.trim(),
        categorySort: Number(d.categorySort) || 0,
        tag: d.tag.trim() || null,
      });
      setPendingId(null);
      if (!res.ok) {
        setError(res.error);
        return;
      }
      setItems((prev) => prev.map((c) => (c.id === res.item.id ? res.item : c)));
      setDrafts((prev) => ({ ...prev, [res.item.id]: toDraft(res.item) }));
    });
  }

  function handleDelete(id: string) {
    if (!confirm("Delete this category?")) return;
    setError(null);
    setPendingId(id);
    startTransition(async () => {
      const res = await deleteCategoryAction(id);
      setPendingId(null);
      if (!res.ok) {
        setError(res.error);
        return;
      }
      setItems((prev) => prev.filter((c) => c.id !== id));
      setDrafts((prev) => {
        const next = { ...prev };
        delete next[id];
        return next;
      });
    });
  }

  return (
    <div className={styles.wrap}>
      <h1 className={styles.heading}>Categories</h1>

      <form className={styles.wrap} onSubmit={handleCreate} style={{ padding: 0 }}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Sort</th>
              <th>Tag</th>
              <th />
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <input name="categoryName" type="text" placeholder="New category" />
              </td>
              <td>
                <input
                  name="categorySort"
                  type="number"
                  defaultValue={0}
                  style={{ width: "5rem" }}
                />
              </td>
              <td>
                <input name="tag" type="text" placeholder="(optional)" />
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
        <p className={styles.empty}>No categories yet.</p>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Sort</th>
              <th>Tag</th>
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
                      value={d.categoryName}
                      onChange={(e) =>
                        setDraft(item.id, { categoryName: e.target.value })
                      }
                      disabled={isPending}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      value={d.categorySort}
                      onChange={(e) =>
                        setDraft(item.id, { categorySort: e.target.value })
                      }
                      disabled={isPending}
                      style={{ width: "5rem" }}
                    />
                  </td>
                  <td>
                    <input
                      type="text"
                      value={d.tag}
                      onChange={(e) => setDraft(item.id, { tag: e.target.value })}
                      disabled={isPending}
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
