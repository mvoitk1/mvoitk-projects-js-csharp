import { beforeEach, describe, expect, it } from "vitest";
import { createTasksRepository } from "../tasksRepository";
import { ETaskStatus, type EntityId } from "../types";
import type { StorageCollection } from "../localStorageGateway";

// What these next lines do:
// Convert date text into normalized ISO strings used by repository date comparisons.
// Why this matters in this project:
// Repository logic compares date strings directly, so consistent formatting avoids flaky tests.
const iso = (value: string) => new Date(value).toISOString();

// What these next lines do:
// Write raw collection envelopes directly into localStorage, including version numbers.
// Why this matters in this project:
// Tests can fully control initial repository state (items + concurrency version).
const seedEnvelope = <T>(collection: StorageCollection, items: T[], version = 1): void => {
  localStorage.setItem(
    `tm.${collection}`,
    JSON.stringify({ version, updatedAt: iso("1970-01-01T00:00:00.000Z"), items }),
  );
};

// What these next lines do:
// Seed the minimum category/priority data that task validation expects.
// Why this matters in this project:
// Most tests focus on filtering/sorting behavior, not FK failures.
const seedBaseFks = (): void => {
  seedEnvelope("categories", [{ id: "c1", name: "Cat" }]);
  seedEnvelope("priorities", [
    { id: "p1", label: "High", value: "high" },
    { id: "p2", label: "Low", value: "low" },
  ]);
};

// What these next lines do:
// Build a valid default task and allow tests to override only the fields they care about.
// Why this matters in this project:
// Keeps test setup short and makes scenario intent easy to read.
const makeTask = (id: EntityId, overrides: Record<string, unknown> = {}) => ({
  id,
  title: id,
  status: ETaskStatus.Todo,
  priorityId: "p1",
  categoryId: "c1",
  createdAt: iso("1970-01-01T00:00:00.000Z"),
  updatedAt: iso("1970-01-01T00:00:00.000Z"),
  dependencyIds: [],
  checklistItemIds: [],
  commentIds: [],
  attachmentIds: [],
  reminderIds: [],
  ...overrides,
});

describe("TasksRepository search/filter/sort and stats", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("filters and sorts by combined criteria", async () => {
    seedBaseFks();
    seedEnvelope("tasks", [
      makeTask("t1", { dueDate: iso("2024-01-02T00:00:00.000Z") }),
      makeTask("t2", { dueDate: iso("2024-01-01T00:00:00.000Z") }),
      makeTask("t3", { status: ETaskStatus.InProgress }),
      makeTask("t4", { priorityId: "p2" }),
    ]);

    const repo = createTasksRepository();
    const result = await repo.query(
      { status: [ETaskStatus.Todo], priorityIds: ["p1"], categoryIds: ["c1"] },
      [{ field: "dueDate", direction: "asc" }],
    );

    expect(result.map((t) => t.id)).toEqual(["t2", "t1"]);
  });

  it("returns dependency-blocked tasks and excludes unblocked when requested", async () => {
    seedBaseFks();
    seedEnvelope("tasks", [
      makeTask("t1"),
      makeTask("t2", { status: ETaskStatus.InProgress }),
    ]);
    seedEnvelope("dependencies", [
      { id: "d1", taskId: "t1", dependsOnId: "t2", type: "blocks" },
    ]);

    const repo = createTasksRepository();
    const blocked = await repo.query({ blocked: true });
    expect(blocked.map((t) => t.id)).toEqual(["t1"]);

    const unblocked = await repo.query({ blocked: false });
    expect(unblocked.map((t) => t.id)).toEqual(["t2"]);
  });

  it("includes recurrence instances inside due window filters", async () => {
    seedBaseFks();
    seedEnvelope("tasks", [makeTask("t1", { recurrenceRuleId: "r1" })]);
    seedEnvelope("recurrence", [
      { id: "r1", taskId: "t1", frequency: "weekly", startDate: iso("1970-01-03T00:00:00.000Z") },
    ]);

    const repo = createTasksRepository();
    const result = await repo.query({ dueAfter: iso("1970-01-02T00:00:00.000Z"), dueBefore: iso("1970-01-04T00:00:00.000Z") });

    expect(result.map((t) => t.id)).toEqual(["t1"]);
  });

  it("derives recurrence-aware overdue/upcoming and blocked stats", async () => {
    seedBaseFks();
    seedEnvelope("tasks", [
      makeTask("t1", { recurrenceRuleId: "r1" }), // should be overdue
      makeTask("t2", { recurrenceRuleId: "r2" }), // should be upcoming
      makeTask("t3"), // becomes blocked by t1
    ]);
    seedEnvelope("recurrence", [
      { id: "r1", taskId: "t1", frequency: "weekly", startDate: iso("1970-01-01T00:00:00.000Z") },
      { id: "r2", taskId: "t2", frequency: "weekly", startDate: iso("1970-01-05T00:00:00.000Z") },
    ]);
    seedEnvelope("dependencies", [
      { id: "d1", taskId: "t3", dependsOnId: "t1", type: "blocks" },
    ]);

    const repo = createTasksRepository();
    const stats = await repo.stats(iso("1970-01-02T00:00:00.000Z"), 7);

    expect(stats.overdue).toBeGreaterThanOrEqual(1);
    expect(stats.upcoming).toBeGreaterThanOrEqual(1);
    expect(stats.blocked).toBe(1);
    expect(stats.total).toBe(3);
  });
});
