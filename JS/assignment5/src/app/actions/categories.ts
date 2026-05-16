"use server";

import { redirect } from "next/navigation";
import { Unauthorized } from "@/lib/api/client";
import {
  createCategory,
  deleteCategory,
  getCategory,
  updateCategory,
} from "@/lib/api/endpoints";
import type {
  TodoCategory,
  TodoCategoryCreate,
} from "@/lib/api/contracts";

export type CategoryResult =
  | { ok: true; item: TodoCategory }
  | { ok: false; error: string };

export type CategoryDeleteResult =
  | { ok: true }
  | { ok: false; error: string };

async function guard<T>(run: () => Promise<T>): Promise<T> {
  try {
    return await run();
  } catch (err) {
    if (err instanceof Unauthorized) redirect("/login");
    throw err;
  }
}

export async function createCategoryAction(
  body: TodoCategoryCreate,
): Promise<CategoryResult> {
  return guard(async () => {
    const res = await createCategory(body);
    if (!res.ok) return { ok: false, error: res.error.message };
    if (res.data) return { ok: true, item: res.data };
    return { ok: false, error: "Server returned no body for created category" };
  });
}

export async function updateCategoryAction(
  current: TodoCategory,
): Promise<CategoryResult> {
  return guard(async () => {
    const put = await updateCategory(current.id, current);
    if (!put.ok) return { ok: false, error: put.error.message };
    const fresh = await getCategory(current.id);
    if (!fresh.ok) return { ok: false, error: fresh.error.message };
    return { ok: true, item: fresh.data };
  });
}

export async function deleteCategoryAction(
  id: string,
): Promise<CategoryDeleteResult> {
  return guard(async () => {
    const res = await deleteCategory(id);
    if (!res.ok) return { ok: false, error: res.error.message };
    return { ok: true };
  });
}
