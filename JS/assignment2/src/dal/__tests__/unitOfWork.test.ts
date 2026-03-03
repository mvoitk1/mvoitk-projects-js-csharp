import { beforeEach, describe, expect, it } from "vitest";
import { createUnitOfWork } from "../unitOfWork";
import { ETaskStatus, type EntityId } from "../types";
import type { StorageCollection } from "../localStorageGateway";

const iso = new Date(0).toISOString();

const seedEnvelope = <T>(collection: StorageCollection, items: T[], version = 1): void => {
  localStorage.setItem(
    `tm.${collection}`,
    JSON.stringify({ version, updatedAt: iso, items }),
  );
};

const resetTasksVersion = (version = 1): void => {
  const raw = localStorage.getItem("tm.tasks");
  const parsed = raw ? JSON.parse(raw) : { items: [] };
  localStorage.setItem(
    "tm.tasks",
    JSON.stringify({ ...parsed, version, updatedAt: iso }),
  );
};

const readItems = (collection: StorageCollection): unknown[] => {
  const raw = localStorage.getItem(`tm.${collection}`);
  const parsed = raw ? JSON.parse(raw) : null;
  return parsed?.items ?? [];
};

const seedBaseFks = (): void => {
  seedEnvelope("categories", [{ id: "c1", name: "Cat" }]);
  seedEnvelope("priorities", [{ id: "p1", label: "P1", value: "high" }]);
};

const makeTask = (id: EntityId, overrides: Record<string, unknown> = {}) => ({
  id,
  title: id,
  status: ETaskStatus.Todo,
  priorityId: "p1",
  categoryId: "c1",
  createdAt: iso,
  updatedAt: iso,
  dependencyIds: [],
  checklistItemIds: [],
  commentIds: [],
  attachmentIds: [],
  reminderIds: [],
  ...overrides,
});

describe("UnitOfWork commit and cascades", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("commits new task across collections", async () => {
    seedBaseFks();
    const uow = createUnitOfWork();

    await uow.tasks.create(makeTask("t1"));
    resetTasksVersion();

    const result = await uow.commit();
    expect(result.success).toBe(true);

    const tasks = readItems("tasks") as { id: string }[];
    expect(tasks.map((t) => t.id)).toEqual(["t1"]);
  });

  it("cascades deletes for task dependents", async () => {
    seedBaseFks();
    seedEnvelope("tasks", [
      makeTask("t1", {
        recurrenceRuleId: "r1",
        dependencyIds: ["d1", "d2"],
        commentIds: ["cm1"],
        attachmentIds: ["a1"],
        reminderIds: ["rm1"],
        checklistItemIds: ["cl1"],
      }),
      makeTask("t2"),
    ]);
    seedEnvelope("dependencies", [
      { id: "d1", taskId: "t2", dependsOnId: "t1", type: "blocks" },
      { id: "d2", taskId: "t1", dependsOnId: "t2", type: "blocks" },
    ]);
    seedEnvelope("recurrence", [{ id: "r1", taskId: "t1", frequency: "weekly", startDate: iso }]);
    seedEnvelope("comments", [{ id: "cm1", taskId: "t1", body: "c", createdAt: iso }]);
    seedEnvelope("attachments", [{ id: "a1", taskId: "t1", url: "u", createdAt: iso }]);
    seedEnvelope("reminders", [{ id: "rm1", taskId: "t1", remindAt: iso, createdAt: iso }]);
    seedEnvelope("checklist", [{ id: "cl1", parentTaskId: "t1", title: "c", completed: false }]);

    const uow = createUnitOfWork();
    await uow.tasks.delete("t1");
    resetTasksVersion();

    const result = await uow.commit();
    expect(result.success).toBe(true);

    expect(readItems("tasks").map((t: any) => t.id)).toEqual(["t2"]);
    expect(readItems("dependencies")).toEqual([]);
    expect(readItems("recurrence")).toEqual([]);
    expect(readItems("comments")).toEqual([]);
    expect(readItems("attachments")).toEqual([]);
    expect(readItems("reminders")).toEqual([]);
    expect(readItems("checklist")).toEqual([]);
  });

  it("rolls back on validation failure", async () => {
    seedBaseFks();
    seedEnvelope("tasks", [makeTask("t1")]);

    const uow = createUnitOfWork();
    await uow.categories.delete("c1");

    const result = await uow.commit();
    expect(result.success).toBe(false);
    expect(result.validationErrors?.length).toBeGreaterThan(0);

    // original data preserved
    expect(readItems("categories").map((c: any) => c.id)).toEqual(["c1"]);
    expect(readItems("tasks").map((t: any) => t.id)).toEqual(["t1"]);
  });

  it("surfaces concurrency conflicts without writing", async () => {
    seedBaseFks();
    seedEnvelope("tasks", []);

    const uow = createUnitOfWork();

    // simulate external writer bumping version
    localStorage.setItem(
      "tm.tasks",
      JSON.stringify({ version: 2, updatedAt: iso, items: [] }),
    );

    const result = await uow.commit();
    expect(result.success).toBe(false);
    expect(result.concurrencyConflict).toBe(true);

    const tasksEnvelope = JSON.parse(localStorage.getItem("tm.tasks") ?? "null");
    expect(tasksEnvelope.version).toBe(2);
    expect(tasksEnvelope.items).toEqual([]);
  });
});
