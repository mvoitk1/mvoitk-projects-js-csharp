import { describe, expect, it } from "vitest";
import {
  mapTaskDtoToDomain,
  mapTaskDomainToDto,
  mapCategoryDtoToDomain,
  mapPriorityDtoToDomain,
  mapDependencyDtoToDomain,
  mapRecurrenceRuleDtoToDomain,
  mapChecklistItemDtoToDomain,
  mapCommentDtoToDomain,
  mapAttachmentDtoToDomain,
  mapReminderDtoToDomain,
  type IForeignKeyContext,
} from "../mappers";
import {
  EDependencyType,
  ERecurrenceFrequency,
  ETaskPriority,
  ETaskStatus,
  type EntityId,
} from "../types";

// What these next lines do:
// Build a fake "allowed IDs" context so mapper functions can validate foreign keys.
// Why this matters in this project:
// We can unit-test mapper validation rules without depending on real repositories/storage.
const fk: IForeignKeyContext = {
  taskIds: new Set<EntityId>(["t1", "t2"]),
  categoryIds: new Set<EntityId>(["c1"]),
  priorityIds: new Set<EntityId>(["p1"]),
  recurrenceRuleIds: new Set<EntityId>(["r1"]),
  dependencyIds: new Set<EntityId>(["d1"]),
  checklistItemIds: new Set<EntityId>(["cl1"]),
  commentIds: new Set<EntityId>(["cm1"]),
  attachmentIds: new Set<EntityId>(["a1"]),
  reminderIds: new Set<EntityId>(["rm1"]),
};

// What these next lines do:
// Create one fully valid task object used as the safe baseline in tests.
// Why this matters in this project:
// Each test can override only one field, so failures clearly point to one rule at a time.
const makeBaseTask = () => ({
  id: "t1",
  title: "Task",
  status: ETaskStatus.Todo,
  priorityId: "p1",
  categoryId: "c1",
  createdAt: new Date(0).toISOString(),
  updatedAt: new Date(0).toISOString(),
});

describe("mappers validation", () => {
  it("maps task DTO with defaults and FK validation", () => {
    const dto = makeBaseTask();
    const result = mapTaskDtoToDomain(dto, fk);

    expect(result.dependencyIds).toEqual([]);
    expect(result.checklistItemIds).toEqual([]);
    expect(result.commentIds).toEqual([]);
    expect(result.attachmentIds).toEqual([]);
    expect(result.reminderIds).toEqual([]);
  });

  it("rejects task with missing required fields", () => {
    const dto = { ...makeBaseTask(), title: "" };
    expect(() => mapTaskDtoToDomain(dto, fk)).toThrow(/task.title is required/);
  });

  it("rejects task with bad FK", () => {
    const dto = { ...makeBaseTask(), priorityId: "missing" };
    expect(() => mapTaskDtoToDomain(dto, fk)).toThrow(/task.priorityId missing does not exist/);
  });

  it("maps category and priority DTOs", () => {
    const category = mapCategoryDtoToDomain({ id: "c1", name: "Cat" });
    const priority = mapPriorityDtoToDomain({ id: "p1", label: "High", value: ETaskPriority.High });

    expect(category.name).toBe("Cat");
    expect(priority.value).toBe(ETaskPriority.High);
  });

  it("validates dependency DTO FKs", () => {
    const dto = { id: "d1", taskId: "t1", dependsOnId: "t2", type: EDependencyType.Blocks };
    expect(() => mapDependencyDtoToDomain(dto, fk)).not.toThrow();
  });

  it("validates recurrence rule DTO FKs", () => {
    const dto = { id: "r1", taskId: "t1", frequency: ERecurrenceFrequency.Weekly };
    expect(() => mapRecurrenceRuleDtoToDomain(dto, fk)).not.toThrow();
  });

  it("validates checklist/comment/attachment/reminder DTOs", () => {
    expect(() =>
      mapChecklistItemDtoToDomain(
        {
          id: "cl1",
          parentTaskId: "t1",
          title: "cl",
          completed: false,
          createdAt: new Date(0).toISOString(),
          updatedAt: new Date(0).toISOString(),
        },
        fk,
      ),
    ).not.toThrow();

    expect(() =>
      mapCommentDtoToDomain({ id: "cm1", taskId: "t1", body: "c", createdAt: new Date(0).toISOString() }, fk),
    ).not.toThrow();

    expect(() =>
      mapAttachmentDtoToDomain({ id: "a1", taskId: "t1", url: "u", createdAt: new Date(0).toISOString() }, fk),
    ).not.toThrow();

    expect(() =>
      mapReminderDtoToDomain({ id: "rm1", taskId: "t1", remindAt: new Date(0).toISOString(), createdAt: new Date(0).toISOString() }, fk),
    ).not.toThrow();
  });

  it("round-trips task domain to DTO", () => {
    const dto = { ...makeBaseTask(), dependencyIds: ["d1"], checklistItemIds: ["cl1"] };
    const domain = mapTaskDtoToDomain(dto, fk);
    const back = mapTaskDomainToDto(domain, fk);

    expect(back).toEqual({ ...dto, commentIds: [], attachmentIds: [], reminderIds: [], tagIds: [], assigneeIds: [] });
  });
});
