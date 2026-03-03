import { loadEnvelope, saveEnvelope } from "./localStorageGateway";
import { compareByFields } from "./utils";
import type {
  EntityId,
  ICategoryDto,
  IDependencyDto,
  IPriorityDto,
  IRecurrenceRuleDto,
  ITaskDto,
  ITaskFilter,
  ITaskSortDescriptor,
  ITaskStats,
  ITasksRepository,
} from "./types";

const PRIORITY_VALUE_ORDER: Record<string, number> = {
  low: 1,
  medium: 2,
  high: 3,
  critical: 4,
};

type ForeignKeySets = {
  categoryIds: Set<EntityId>;
  priorityIds: Set<EntityId>;
  recurrenceById: Map<EntityId, IRecurrenceRuleDto>;
  dependencyById: Map<EntityId, IDependencyDto>;
};

const cloneTask = (task: ITaskDto): ITaskDto => ({
  ...task,
  dependencyIds: [...(task.dependencyIds ?? [])],
  checklistItemIds: [...(task.checklistItemIds ?? [])],
  commentIds: [...(task.commentIds ?? [])],
  attachmentIds: [...(task.attachmentIds ?? [])],
  reminderIds: [...(task.reminderIds ?? [])],
  tagIds: [...(task.tagIds ?? [])],
  assigneeIds: [...(task.assigneeIds ?? [])],
});

const ensure = (condition: boolean, message: string): void => {
  if (!condition) {
    throw new Error(message);
  }
};

const buildFkSets = (): ForeignKeySets => {
  const categories = loadEnvelope<ICategoryDto>("categories").items;
  const priorities = loadEnvelope<IPriorityDto>("priorities").items;
  const recurrence = loadEnvelope<IRecurrenceRuleDto>("recurrence").items;
  const dependencies = loadEnvelope<IDependencyDto>("dependencies").items;

  return {
    categoryIds: new Set(categories.map((c) => c.id)),
    priorityIds: new Set(priorities.map((p) => p.id)),
    recurrenceById: new Map(recurrence.map((r) => [r.id, r])),
    dependencyById: new Map(dependencies.map((d) => [d.id, d])),
  };
};

const hydrateTask = (incoming: ITaskDto, fallback: ITaskDto | undefined): ITaskDto => {
  const now = new Date().toISOString();
  const base = fallback ? cloneTask(fallback) : undefined;

  return {
    ...base,
    ...incoming,
    createdAt: incoming.createdAt ?? base?.createdAt ?? now,
    updatedAt: now,
    dependencyIds: incoming.dependencyIds ?? base?.dependencyIds ?? [],
    checklistItemIds: incoming.checklistItemIds ?? base?.checklistItemIds ?? [],
    commentIds: incoming.commentIds ?? base?.commentIds ?? [],
    attachmentIds: incoming.attachmentIds ?? base?.attachmentIds ?? [],
    reminderIds: incoming.reminderIds ?? base?.reminderIds ?? [],
    tagIds: incoming.tagIds ?? base?.tagIds ?? [],
    assigneeIds: incoming.assigneeIds ?? base?.assigneeIds ?? [],
  };
};

const validateTask = (task: ITaskDto, fk: ForeignKeySets): void => {
  ensure(!!task.id, "task.id is required");
  ensure(!!task.title, "task.title is required");
  ensure(!!task.status, "task.status is required");
  ensure(!!task.priorityId, "task.priorityId is required");
  ensure(!!task.categoryId, "task.categoryId is required");
  ensure(!!task.createdAt, "task.createdAt is required");
  ensure(!!task.updatedAt, "task.updatedAt is required");

  ensure(fk.categoryIds.has(task.categoryId), `task.categoryId ${task.categoryId} does not exist`);
  ensure(fk.priorityIds.has(task.priorityId), `task.priorityId ${task.priorityId} does not exist`);

   if (task.recurrenceRuleId) {
     const rule = fk.recurrenceById.get(task.recurrenceRuleId);
     ensure(!!rule, `task.recurrenceRuleId ${task.recurrenceRuleId} does not exist`);
     ensure(rule?.taskId === task.id, `recurrenceRuleId ${task.recurrenceRuleId} does not belong to task ${task.id}`);
   }

   (task.dependencyIds ?? []).forEach((depId) => {
     const edge = fk.dependencyById.get(depId);
     ensure(!!edge, `dependency ${depId} does not exist`);
     ensure(edge?.taskId === task.id, `dependency ${depId} does not belong to task ${task.id}`);
   });
};

export class TasksRepository implements ITasksRepository {
  private items: ITaskDto[];
  private version: number;
  private indexesBuilt = false;
  private lastDependenciesVersion = -1;
  private lastRecurrenceVersion = -1;
  private lastPrioritiesVersion = -1;
  private lastTasksVersion = -1;

  private byCategory = new Map<EntityId, ITaskDto[]>();
  private byPriority = new Map<EntityId, ITaskDto[]>();
  private byStatus = new Map<string, ITaskDto[]>();
  private byTag = new Map<EntityId, ITaskDto[]>();
  private byDueBucket = new Map<string, ITaskDto[]>();
  private recurrenceIds = new Set<EntityId>();
  private blockedIds = new Set<EntityId>();
  private dependenciesByTask = new Map<EntityId, IDependencyDto[]>();
  private recurrenceRulesByTask = new Map<EntityId, IRecurrenceRuleDto>();
  private priorityWeights = new Map<EntityId, number>();

  private buildIndexes(): void {
    const dependencyEnvelope = loadEnvelope<IDependencyDto>("dependencies");
    const recurrenceEnvelope = loadEnvelope<IRecurrenceRuleDto>("recurrence");
    const prioritiesEnvelope = loadEnvelope<IPriorityDto>("priorities");

    const shouldRebuild =
      !this.indexesBuilt ||
      this.lastDependenciesVersion !== dependencyEnvelope.version ||
      this.lastRecurrenceVersion !== recurrenceEnvelope.version ||
      this.lastPrioritiesVersion !== prioritiesEnvelope.version ||
      this.lastTasksVersion !== this.version;

    if (!shouldRebuild) return;

    this.lastDependenciesVersion = dependencyEnvelope.version;
    this.lastRecurrenceVersion = recurrenceEnvelope.version;
    this.lastPrioritiesVersion = prioritiesEnvelope.version;
    this.lastTasksVersion = this.version;

    this.byCategory.clear();
    this.byPriority.clear();
    this.byStatus.clear();
    this.byTag.clear();
    this.byDueBucket.clear();
    this.recurrenceIds.clear();
    this.blockedIds.clear();
    this.dependenciesByTask.clear();
    this.recurrenceRulesByTask.clear();
    this.priorityWeights.clear();

    const dependencyEdges = dependencyEnvelope.items;
    dependencyEdges.forEach((edge) => {
      const list = this.dependenciesByTask.get(edge.taskId) ?? [];
      list.push(edge);
      this.dependenciesByTask.set(edge.taskId, list);
    });

    const recurrenceRules = recurrenceEnvelope.items;
    recurrenceRules.forEach((rule) => {
      this.recurrenceRulesByTask.set(rule.taskId, rule);
    });

    prioritiesEnvelope.items.forEach((priority) => {
      const weight = PRIORITY_VALUE_ORDER[priority.value as keyof typeof PRIORITY_VALUE_ORDER];
      this.priorityWeights.set(priority.id, weight ?? Number.MAX_SAFE_INTEGER);
    });

    const dependencyLookup = new Map(this.items.map((t) => [t.id, t] as const));
    const now = new Date().toISOString();

    for (const task of this.items) {
      const push = (map: Map<EntityId, ITaskDto[]>, key: EntityId, value: ITaskDto): void => {
        const arr = map.get(key) ?? [];
        arr.push(cloneTask(value));
        map.set(key, arr);
      };

      push(this.byCategory, task.categoryId, task);
      push(this.byPriority, task.priorityId, task);

      const statusArr = this.byStatus.get(task.status) ?? [];
      statusArr.push(cloneTask(task));
      this.byStatus.set(task.status, statusArr);

      (task.tagIds ?? []).forEach((tagId) => push(this.byTag, tagId, task));

      const effectiveDue = this.getEffectiveDueDate(task);
      const recurrenceRule = this.recurrenceRulesByTask.get(task.id);

      let bucket = "none";
      if (effectiveDue) {
        bucket = effectiveDue < now ? "overdue" : "upcoming";
      } else if (recurrenceRule) {
        if (this.hasRecurrenceInstanceInWindow(recurrenceRule, undefined, now)) {
          bucket = "overdue";
        } else if (this.hasRecurrenceInstanceInWindow(recurrenceRule, now, undefined)) {
          bucket = "upcoming";
        }
      }
      const bucketArr = this.byDueBucket.get(bucket) ?? [];
      bucketArr.push(cloneTask(task));
      this.byDueBucket.set(bucket, bucketArr);

      if (task.recurrenceRuleId || this.recurrenceRulesByTask.has(task.id)) {
        this.recurrenceIds.add(task.id);
      }

      if (this.isBlocked(task, dependencyLookup)) {
        this.blockedIds.add(task.id);
      }
    }

    this.indexesBuilt = true;
  }

  private invalidateIndexes(): void {
    this.indexesBuilt = false;
  }

  constructor() {
    const envelope = loadEnvelope<ITaskDto>("tasks");
    this.items = envelope.items.map(cloneTask);
    this.version = envelope.version;
    this.invalidateIndexes();
  }

  private async persist(next: ITaskDto[]): Promise<void> {
    const envelope = saveEnvelope<ITaskDto>("tasks", next, this.version);
    this.items = envelope.items.map(cloneTask);
    this.version = envelope.version;
    this.invalidateIndexes();
  }

  async findAll(): Promise<ITaskDto[]> {
    this.buildIndexes();
    return this.items.map(cloneTask);
  }

  async findById(id: EntityId): Promise<ITaskDto | undefined> {
    this.buildIndexes();
    const found = this.items.find((task) => task.id === id);
    return found ? cloneTask(found) : undefined;
  }

  async findByCategory(categoryId: EntityId): Promise<ITaskDto[]> {
    this.buildIndexes();
    const list = this.byCategory.get(categoryId) ?? [];
    return list.map(cloneTask);
  }

  async findByPriority(priorityId: EntityId): Promise<ITaskDto[]> {
    this.buildIndexes();
    const list = this.byPriority.get(priorityId) ?? [];
    return list.map(cloneTask);
  }

  async create(dto: ITaskDto): Promise<ITaskDto> {
    const fk = buildFkSets();
    const candidate = hydrateTask(dto, undefined);
    validateTask(candidate, fk);

    ensure(
      !this.items.some((task) => task.id === candidate.id),
      `Entity with id ${candidate.id} already exists`,
    );

    const next = [...this.items, candidate].map(cloneTask);
    await this.persist(next);
    return cloneTask(candidate);
  }

  async update(dto: ITaskDto): Promise<ITaskDto> {
    const fk = buildFkSets();
    const existing = this.items.find((task) => task.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const candidate = hydrateTask(dto, existing);
    validateTask(candidate, fk);

    const next = this.items.map((task) => (task.id === candidate.id ? candidate : task)).map(cloneTask);
    await this.persist(next);
    return cloneTask(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    const next = this.items.filter((task) => task.id !== id).map(cloneTask);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }

  private isBlocked(task: ITaskDto, dependencyLookup: Map<EntityId, ITaskDto>): boolean {
    const edges = this.dependenciesByTask.get(task.id) ?? [];
    if (edges.length === 0) return false;

    return edges.some((edge) => {
      const dependsOn = dependencyLookup.get(edge.dependsOnId);
      if (!dependsOn) return false;
      return dependsOn.status !== "completed" && dependsOn.status !== "cancelled";
    });
  }

  private addInterval(base: Date, frequency: IRecurrenceRuleDto["frequency"], interval: number): Date {
    const next = new Date(base);
    switch (frequency) {
      case "daily":
        next.setDate(next.getDate() + interval);
        break;
      case "weekly":
        next.setDate(next.getDate() + interval * 7);
        break;
      case "monthly":
        next.setMonth(next.getMonth() + interval);
        break;
      case "yearly":
        next.setFullYear(next.getFullYear() + interval);
        break;
      default:
        break;
    }
    return next;
  }

  private hasRecurrenceInstanceInWindow(
    rule: IRecurrenceRuleDto,
    windowStart?: string,
    windowEnd?: string,
  ): boolean {
    if (!rule.startDate) return false;

    const interval = Math.max(1, rule.interval ?? 1);
    const maxCount = rule.count;
    const untilTs = rule.until ? new Date(rule.until).getTime() : undefined;
    const windowStartTs = windowStart ? new Date(windowStart).getTime() : undefined;
    const windowEndTs = windowEnd ? new Date(windowEnd).getTime() : undefined;

    let occurrenceIndex = 0;
    let current = new Date(rule.startDate);

    while (true) {
      if (maxCount !== undefined && occurrenceIndex >= maxCount) return false;
      const currentTs = current.getTime();
      if (untilTs !== undefined && currentTs > untilTs) return false;

      const inStartRange = windowStartTs === undefined || currentTs >= windowStartTs;
      const inEndRange = windowEndTs === undefined || currentTs <= windowEndTs;
      if (inStartRange && inEndRange) {
        return true;
      }

      if (windowEndTs !== undefined && currentTs > windowEndTs) return false;

      occurrenceIndex += 1;
      const next = this.addInterval(current, rule.frequency, interval);
      if (next.getTime() === currentTs) return false;
      current = next;
      if (occurrenceIndex > 512) return false;
    }
  }

  private getEffectiveDueDate(task: ITaskDto): string | undefined {
    const recurrence = this.recurrenceRulesByTask.get(task.id);
    if (recurrence?.startDate) {
      return recurrence.startDate;
    }
    return task.dueDate;
  }

  private applyFilter(tasks: ITaskDto[], filter?: ITaskFilter): ITaskDto[] {
    if (!filter) return tasks;

    const matchesDateRange = (value: string | undefined, after?: string, before?: string): boolean => {
      const isAfter = (val: string | undefined, cutoff: string | undefined) => (cutoff ? !!val && val >= cutoff : true);
      const isBefore = (val: string | undefined, cutoff: string | undefined) => (cutoff ? !!val && val <= cutoff : true);
      return isAfter(value, after) && isBefore(value, before);
    };

    return tasks.filter((task) => {
      if (filter.status && !filter.status.includes(task.status)) return false;
      if (filter.priorityIds && !filter.priorityIds.includes(task.priorityId)) return false;
      if (filter.categoryIds && !filter.categoryIds.includes(task.categoryId)) return false;
      if (filter.tagIds && !(task.tagIds ?? []).some((id) => filter.tagIds!.includes(id))) return false;
      if (filter.assigneeIds && !(task.assigneeIds ?? []).some((id) => filter.assigneeIds!.includes(id))) return false;

      const effectiveDue = this.getEffectiveDueDate(task);
      const recurrenceRule = this.recurrenceRulesByTask.get(task.id);

      if (filter.dueAfter || filter.dueBefore) {
        const matchesDue =
          matchesDateRange(effectiveDue, filter.dueAfter, filter.dueBefore) ||
          (!!recurrenceRule && this.hasRecurrenceInstanceInWindow(recurrenceRule, filter.dueAfter, filter.dueBefore));
        if (!matchesDue) return false;
      }

      if (!matchesDateRange(task.createdAt, filter.createdAfter, filter.createdBefore)) return false;
      if (!matchesDateRange(task.updatedAt, filter.updatedAfter, filter.updatedBefore)) return false;

      const hasRecurrence = task.recurrenceRuleId !== undefined || this.recurrenceRulesByTask.has(task.id);
      if (filter.hasRecurrence === true && !hasRecurrence) return false;
      if (filter.hasRecurrence === false && hasRecurrence) return false;

      const dependencyLookup = new Map(this.items.map((t) => [t.id, t] as const));
      if (filter.blocked === true && !this.isBlocked(task, dependencyLookup)) return false;
      if (filter.blocked === false && this.isBlocked(task, dependencyLookup)) return false;

      return true;
    });
  }

  private applySort(tasks: ITaskDto[], sorts?: ITaskSortDescriptor[]): ITaskDto[] {
    if (!sorts || sorts.length === 0) return tasks;
    const comparator = compareByFields<ITaskDto>(
      sorts.map((s) => ({
        selector: (task) => {
          switch (s.field) {
            case "dueDate":
              return this.getEffectiveDueDate(task) ?? "";
            case "priority":
              return this.priorityWeights.get(task.priorityId) ?? Number.MAX_SAFE_INTEGER;
            case "createdAt":
            case "updatedAt":
              return task[s.field];
            default:
              return task[s.field as keyof ITaskDto] as unknown;
          }
        },
        direction: s.direction ?? "asc",
      })),
    );
    return [...tasks].sort(comparator);
  }

  async query(filter?: ITaskFilter, sorts?: ITaskSortDescriptor[]): Promise<ITaskDto[]> {
    this.buildIndexes();
    const filtered = this.applyFilter(this.items, filter);
    const sorted = this.applySort(filtered, sorts);
    return sorted.map(cloneTask);
  }

  async stats(now: string, upcomingWindowDays = 7): Promise<ITaskStats> {
    this.buildIndexes();
    const nowDate = new Date(now).toISOString();
    const upcomingCutoff = new Date(now);
    upcomingCutoff.setDate(upcomingCutoff.getDate() + upcomingWindowDays);
    const upcomingIso = upcomingCutoff.toISOString();

    const byStatus: Record<string, number> = {} as Record<string, number>;
    const byPriority: Record<string, number> = {};
    const byCategory: Record<string, number> = {};

    const dependencyLookup = new Map(this.items.map((t) => [t.id, t] as const));

    let overdue = 0;
    let upcoming = 0;
    let blocked = 0;

    for (const task of this.items) {
      byStatus[task.status] = (byStatus[task.status] ?? 0) + 1;
      byPriority[task.priorityId] = (byPriority[task.priorityId] ?? 0) + 1;
      byCategory[task.categoryId] = (byCategory[task.categoryId] ?? 0) + 1;

      const effectiveDue = this.getEffectiveDueDate(task);
      const recurrenceRule = this.recurrenceRulesByTask.get(task.id);
      const isCompleted = task.status === "completed" || task.status === "cancelled";

      let countedOverdue = false;
      let countedUpcoming = false;

      if (effectiveDue && !isCompleted && effectiveDue < nowDate) {
        overdue += 1;
        countedOverdue = true;
      }
      if (effectiveDue && !isCompleted && effectiveDue >= nowDate && effectiveDue <= upcomingIso) {
        upcoming += 1;
        countedUpcoming = true;
      }

      if (!isCompleted && recurrenceRule) {
        if (!countedOverdue && this.hasRecurrenceInstanceInWindow(recurrenceRule, undefined, nowDate)) {
          overdue += 1;
          countedOverdue = true;
        }
        if (!countedUpcoming && this.hasRecurrenceInstanceInWindow(recurrenceRule, nowDate, upcomingIso)) {
          upcoming += 1;
          countedUpcoming = true;
        }
      }

      if (this.isBlocked(task, dependencyLookup)) {
        blocked += 1;
      }
    }

    return {
      total: this.items.length,
      byStatus: byStatus as Record<ITaskDto["status"], number>,
      byPriority,
      byCategory,
      overdue,
      upcoming,
      blocked,
    };
  }
}

export const createTasksRepository = (): ITasksRepository => new TasksRepository();

// Adapter that lets the UnitOfWork reuse the same logic with supplied initial items
export class TasksRepositoryAdapter extends TasksRepository {
  constructor(initial: ITaskDto[]) {
    super();
    // override loaded items/version with provided snapshot without persisting
    // (used only inside UnitOfWork where snapshots are already loaded)
    (this as unknown as { items: ITaskDto[] }).items = initial.map(cloneTask);
  }
}
