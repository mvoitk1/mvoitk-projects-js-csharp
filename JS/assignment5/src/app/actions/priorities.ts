"use server";

import { redirect } from "next/navigation";
import { Unauthorized } from "@/lib/api/client";
import {
  createPriority,
  deletePriority,
  getPriority,
  updatePriority,
} from "@/lib/api/endpoints";
import type {
  TodoPriority,
  TodoPriorityCreate,
} from "@/lib/api/contracts";

export type PriorityResult =
  | { ok: true; item: TodoPriority }
  | { ok: false; error: string };

export type PriorityDeleteResult =
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

export async function createPriorityAction(
  body: TodoPriorityCreate,
): Promise<PriorityResult> {
  return guard(async () => {
    const res = await createPriority(body);
    if (!res.ok) return { ok: false, error: res.error.message };
    if (res.data) return { ok: true, item: res.data };
    return { ok: false, error: "Server returned no body for created priority" };
  });
}

export async function updatePriorityAction(
  current: TodoPriority,
): Promise<PriorityResult> {
  return guard(async () => {
    const put = await updatePriority(current.id, current);
    if (!put.ok) return { ok: false, error: put.error.message };
    const fresh = await getPriority(current.id);
    if (!fresh.ok) return { ok: false, error: fresh.error.message };
    return { ok: true, item: fresh.data };
  });
}

export async function deletePriorityAction(
  id: string,
): Promise<PriorityDeleteResult> {
  return guard(async () => {
    const res = await deletePriority(id);
    if (!res.ok) return { ok: false, error: res.error.message };
    return { ok: true };
  });
}
