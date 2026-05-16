"use server";

import { redirect } from "next/navigation";
import { Unauthorized } from "@/lib/api/client";
import {
  createTask,
  deleteTask,
  getTask,
  updateTask,
} from "@/lib/api/endpoints";
import type {
  TodoTask,
  TodoTaskCreate,
  TodoTaskUpdate,
} from "@/lib/api/contracts";

export type TaskMutationResult =
  | { ok: true; item: TodoTask }
  | { ok: false; error: string };

export type TaskDeleteResult = { ok: true } | { ok: false; error: string };

async function guard<T>(run: () => Promise<T>): Promise<T> {
  try {
    return await run();
  } catch (err) {
    if (err instanceof Unauthorized) redirect("/login");
    throw err;
  }
}

export async function createTaskAction(
  body: TodoTaskCreate,
): Promise<TaskMutationResult> {
  return guard(async () => {
    const res = await createTask(body);
    if (!res.ok) return { ok: false, error: res.error.message };
    // Some backends return 204; fall back to GET if needed.
    if (res.data) return { ok: true, item: res.data };
    return { ok: false, error: "Server returned no body for created task" };
  });
}

export async function updateTaskAction(
  id: string,
  body: TodoTaskUpdate,
): Promise<TaskMutationResult> {
  return guard(async () => {
    const put = await updateTask(id, body);
    if (!put.ok) return { ok: false, error: put.error.message };
    // PUT usually returns 204 — refetch to get the new syncDt for next edits.
    const fresh = await getTask(id);
    if (!fresh.ok) return { ok: false, error: fresh.error.message };
    return { ok: true, item: fresh.data };
  });
}

export async function deleteTaskAction(
  id: string,
): Promise<TaskDeleteResult> {
  return guard(async () => {
    const res = await deleteTask(id);
    if (!res.ok) return { ok: false, error: res.error.message };
    return { ok: true };
  });
}
