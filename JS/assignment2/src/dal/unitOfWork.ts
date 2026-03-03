import { ConcurrencyError, loadEnvelope, saveEnvelope } from "./localStorageGateway";
import { createCrudRepository } from "./utils";
import type {
  EntityId,
  IAttachmentDto,
  IAttachmentsRepository,
  ICategoriesRepository,
  ICategoryDto,
  IChecklistItemDto,
  IChecklistRepository,
  ICommentDto,
  ICommentsRepository,
  IDependenciesRepository,
  IDependencyDto,
  IPrioritiesRepository,
  IPriorityDto,
  IRecurrenceRepository,
  IRecurrenceRuleDto,
  IReminderDto,
  IRemindersRepository,
  ITaskDto,
  ITasksRepository,
  IUnitOfWork,
  IUnitOfWorkCommitResult,
} from "./types";
import { TasksRepositoryAdapter } from "./tasksRepository";

type CollectionKey =
  | "tasks"
  | "categories"
  | "priorities"
  | "dependencies"
  | "recurrence"
  | "comments"
  | "attachments"
  | "reminders"
  | "checklist";

type Snapshot<TDto> = { items: TDto[]; version: number };

type SnapshotMap = {
  tasks: Snapshot<ITaskDto>;
  categories: Snapshot<ICategoryDto>;
  priorities: Snapshot<IPriorityDto>;
  dependencies: Snapshot<IDependencyDto>;
  recurrence: Snapshot<IRecurrenceRuleDto>;
  comments: Snapshot<ICommentDto>;
  attachments: Snapshot<IAttachmentDto>;
  reminders: Snapshot<IReminderDto>;
  checklist: Snapshot<IChecklistItemDto>;
};

type Changeset<TDto> = {
  created: TDto[];
  updated: TDto[];
  deleted: EntityId[];
};

const cloneArray = <T>(items: T[]): T[] => items.map((item) => ({ ...(item as object) } as T));

const ensure = (condition: boolean, message: string): void => {
  if (!condition) {
    throw new Error(message);
  }
};

const loadSnapshot = <TDto>(collection: CollectionKey): Snapshot<TDto> => {
  const envelope = loadEnvelope<TDto>(collection as never);
  return { items: cloneArray(envelope.items), version: envelope.version };
};

const deepEqual = <T>(a: T, b: T): boolean => JSON.stringify(a) === JSON.stringify(b);

const computeChangeset = <TDto>(
  base: TDto[],
  next: TDto[],
  getId: (dto: TDto) => EntityId,
): Changeset<TDto> => {
  const baseMap = new Map<EntityId, TDto>();
  base.forEach((item) => baseMap.set(getId(item), item));

  const nextMap = new Map<EntityId, TDto>();
  next.forEach((item) => nextMap.set(getId(item), item));

  const created: TDto[] = [];
  const updated: TDto[] = [];
  const deleted: EntityId[] = [];

  nextMap.forEach((item, id) => {
    if (!baseMap.has(id)) {
      created.push(item);
    } else if (!deepEqual(baseMap.get(id), item)) {
      updated.push(item);
    }
  });

  baseMap.forEach((_item, id) => {
    if (!nextMap.has(id)) {
      deleted.push(id);
    }
  });

  return { created, updated, deleted };
};

const hasChanges = <TDto>(changes: Changeset<TDto>): boolean =>
  changes.created.length > 0 || changes.updated.length > 0 || changes.deleted.length > 0;

const makeCrud = <TDto>(initialItems: TDto[]) =>
  createCrudRepository<TDto>({
    getId: (dto) => (dto as { id: EntityId }).id,
    initialItems,
  });

const makeTasksRepository = (initial: ITaskDto[]): ITasksRepository => new TasksRepositoryAdapter(initial);

const makeChecklistRepository = (initial: IChecklistItemDto[]): IChecklistRepository => makeCrud(initial);

const buildTaskIdSet = (tasks: ITaskDto[]): Set<EntityId> => new Set(tasks.map((task) => task.id));

const buildDependencyAdjacency = (edges: IDependencyDto[]): Map<EntityId, EntityId[]> => {
  const adjacency = new Map<EntityId, EntityId[]>();
  edges.forEach((edge) => {
    const list = adjacency.get(edge.taskId) ?? [];
    list.push(edge.dependsOnId);
    adjacency.set(edge.taskId, list);
  });
  return adjacency;
};

const hasDependencyCycle = (edges: IDependencyDto[]): boolean => {
  const adjacency = buildDependencyAdjacency(edges);
  const visiting = new Set<EntityId>();
  const visited = new Set<EntityId>();

  const dfs = (node: EntityId): boolean => {
    if (visiting.has(node)) return true;
    if (visited.has(node)) return false;
    visiting.add(node);
    const neighbors = adjacency.get(node) ?? [];
    for (const neighbor of neighbors) {
      if (dfs(neighbor)) return true;
    }
    visiting.delete(node);
    visited.add(node);
    return false;
  };

  for (const node of adjacency.keys()) {
    if (dfs(node)) return true;
  }
  return false;
};

const cleanIds = (ids: EntityId[] | undefined, allowed: Set<EntityId>): EntityId[] =>
  (ids ?? []).filter((id) => allowed.has(id));

const applyCascadeDeletes = (state: SnapshotMap, baseline: SnapshotMap): void => {
  const baselineTaskIds = buildTaskIdSet(baseline.tasks.items);
  const nextTaskIds = buildTaskIdSet(state.tasks.items);
  const deletedTaskIds = new Set<EntityId>();
  baselineTaskIds.forEach((id) => {
    if (!nextTaskIds.has(id)) {
      deletedTaskIds.add(id);
    }
  });

  if (deletedTaskIds.size === 0) {
    return;
  }

  state.dependencies.items = state.dependencies.items.filter(
    (edge) => !deletedTaskIds.has(edge.taskId) && !deletedTaskIds.has(edge.dependsOnId),
  );

  const removedRecurrenceRuleIds = new Set<EntityId>();
  state.recurrence.items = state.recurrence.items.filter((rule) => {
    const keep = !deletedTaskIds.has(rule.taskId);
    if (!keep) {
      removedRecurrenceRuleIds.add(rule.id);
    }
    return keep;
  });

  state.comments.items = state.comments.items.filter((comment) => !deletedTaskIds.has(comment.taskId));
  state.attachments.items = state.attachments.items.filter(
    (attachment) => !deletedTaskIds.has(attachment.taskId),
  );
  state.reminders.items = state.reminders.items.filter((reminder) => !deletedTaskIds.has(reminder.taskId));
  state.checklist.items = state.checklist.items.filter(
    (item) => !deletedTaskIds.has(item.parentTaskId),
  );

  const dependencyIds = new Set(state.dependencies.items.map((edge) => edge.id));
  const recurrenceIds = new Set(state.recurrence.items.map((rule) => rule.id));
  const commentIds = new Set(state.comments.items.map((comment) => comment.id));
  const attachmentIds = new Set(state.attachments.items.map((attachment) => attachment.id));
  const reminderIds = new Set(state.reminders.items.map((reminder) => reminder.id));
  const checklistIds = new Set(state.checklist.items.map((item) => item.id));

  state.tasks.items = state.tasks.items.map((task) => {
    const recurrenceRuleId =
      task.recurrenceRuleId && recurrenceIds.has(task.recurrenceRuleId)
        ? task.recurrenceRuleId
        : undefined;

    return {
      ...task,
      recurrenceRuleId,
      dependencyIds: cleanIds(task.dependencyIds, dependencyIds),
      checklistItemIds: cleanIds(task.checklistItemIds, checklistIds),
      commentIds: cleanIds(task.commentIds, commentIds),
      attachmentIds: cleanIds(task.attachmentIds, attachmentIds),
      reminderIds: cleanIds(task.reminderIds, reminderIds),
    };
  });
};

const validateState = (state: SnapshotMap): string[] => {
  const errors: string[] = [];

  const collect = (fn: () => void): void => {
    try {
      fn();
    } catch (error) {
      errors.push(String(error));
    }
  };

  const categoryIds = new Set(state.categories.items.map((c) => c.id));
  const priorityIds = new Set(state.priorities.items.map((p) => p.id));
  const taskIds = buildTaskIdSet(state.tasks.items);
  const recurrenceById = new Map(state.recurrence.items.map((rule) => [rule.id, rule] as const));

  state.tasks.items.forEach((task) => {
    collect(() => {
      ensure(!!task.id, "task.id is required");
      ensure(!!task.title, "task.title is required");
      ensure(!!task.status, "task.status is required");
      ensure(!!task.priorityId, "task.priorityId is required");
      ensure(!!task.categoryId, "task.categoryId is required");
      ensure(!!task.createdAt, "task.createdAt is required");
      ensure(!!task.updatedAt, "task.updatedAt is required");
      ensure(priorityIds.has(task.priorityId), `task.priorityId ${task.priorityId} does not exist`);
      ensure(categoryIds.has(task.categoryId), `task.categoryId ${task.categoryId} does not exist`);
      if (task.recurrenceRuleId) {
        const rule = recurrenceById.get(task.recurrenceRuleId);
        ensure(!!rule, `task.recurrenceRuleId ${task.recurrenceRuleId} does not exist`);
        ensure(
          rule?.taskId === task.id,
          `task.recurrenceRuleId ${task.recurrenceRuleId} does not belong to task ${task.id}`,
        );
      }
    });
  });

  state.recurrence.items.forEach((rule) => {
    collect(() => {
      ensure(!!rule.id, "recurrenceRule.id is required");
      ensure(!!rule.taskId, "recurrenceRule.taskId is required");
      ensure(!!rule.frequency, "recurrenceRule.frequency is required");
      ensure(taskIds.has(rule.taskId), `recurrenceRule.taskId ${rule.taskId} does not exist`);
    });
  });

  state.dependencies.items.forEach((edge) => {
    collect(() => {
      ensure(!!edge.id, "dependency.id is required");
      ensure(!!edge.taskId, "dependency.taskId is required");
      ensure(!!edge.dependsOnId, "dependency.dependsOnId is required");
      ensure(!!edge.type, "dependency.type is required");
      ensure(edge.taskId !== edge.dependsOnId, "dependency cannot self-reference");
      ensure(taskIds.has(edge.taskId), `dependency.taskId ${edge.taskId} does not exist`);
      ensure(taskIds.has(edge.dependsOnId), `dependency.dependsOnId ${edge.dependsOnId} does not exist`);
    });
  });

  if (hasDependencyCycle(state.dependencies.items)) {
    errors.push("dependency cycle detected");
  }

  state.comments.items.forEach((comment) => {
    collect(() => {
      ensure(!!comment.id, "comment.id is required");
      ensure(!!comment.taskId, "comment.taskId is required");
      ensure(!!comment.body, "comment.body is required");
      ensure(!!comment.createdAt, "comment.createdAt is required");
      ensure(taskIds.has(comment.taskId), `comment.taskId ${comment.taskId} does not exist`);
    });
  });

  state.attachments.items.forEach((attachment) => {
    collect(() => {
      ensure(!!attachment.id, "attachment.id is required");
      ensure(!!attachment.taskId, "attachment.taskId is required");
      ensure(!!attachment.url, "attachment.url is required");
      ensure(!!attachment.createdAt, "attachment.createdAt is required");
      ensure(taskIds.has(attachment.taskId), `attachment.taskId ${attachment.taskId} does not exist`);
    });
  });

  state.reminders.items.forEach((reminder) => {
    collect(() => {
      ensure(!!reminder.id, "reminder.id is required");
      ensure(!!reminder.taskId, "reminder.taskId is required");
      ensure(!!reminder.remindAt, "reminder.remindAt is required");
      ensure(!!reminder.createdAt, "reminder.createdAt is required");
      ensure(taskIds.has(reminder.taskId), `reminder.taskId ${reminder.taskId} does not exist`);
    });
  });

  state.checklist.items.forEach((item) => {
    collect(() => {
      ensure(!!item.id, "checklist.id is required");
      ensure(!!item.parentTaskId, "checklist.parentTaskId is required");
      ensure(!!item.title, "checklist.title is required");
      ensure(taskIds.has(item.parentTaskId), `checklist.parentTaskId ${item.parentTaskId} does not exist`);
    });
  });

  return errors;
};

export class UnitOfWork implements IUnitOfWork {
  public tasks: ITasksRepository;
  public categories: ICategoriesRepository;
  public priorities: IPrioritiesRepository;
  public dependencies: IDependenciesRepository;
  public recurrence: IRecurrenceRepository;
  public comments: ICommentsRepository;
  public attachments: IAttachmentsRepository;
  public reminders: IRemindersRepository;
  public checklist: IChecklistRepository;

  private snapshots: SnapshotMap;

  constructor() {
    this.snapshots = {
      tasks: loadSnapshot<ITaskDto>("tasks"),
      categories: loadSnapshot<ICategoryDto>("categories"),
      priorities: loadSnapshot<IPriorityDto>("priorities"),
      dependencies: loadSnapshot<IDependencyDto>("dependencies"),
      recurrence: loadSnapshot<IRecurrenceRuleDto>("recurrence"),
      comments: loadSnapshot<ICommentDto>("comments"),
      attachments: loadSnapshot<IAttachmentDto>("attachments"),
      reminders: loadSnapshot<IReminderDto>("reminders"),
      checklist: loadSnapshot<IChecklistItemDto>("checklist"),
    };

    this.tasks = makeTasksRepository(this.snapshots.tasks.items);
    this.categories = makeCrud<ICategoryDto>(this.snapshots.categories.items);
    this.priorities = makeCrud<IPriorityDto>(this.snapshots.priorities.items);
    this.dependencies = makeCrud<IDependencyDto>(this.snapshots.dependencies.items);
    this.recurrence = makeCrud<IRecurrenceRuleDto>(this.snapshots.recurrence.items);
    this.comments = makeCrud<ICommentDto>(this.snapshots.comments.items);
    this.attachments = makeCrud<IAttachmentDto>(this.snapshots.attachments.items);
    this.reminders = makeCrud<IReminderDto>(this.snapshots.reminders.items);
    this.checklist = makeChecklistRepository(this.snapshots.checklist.items);
  }

  private async currentState(): Promise<SnapshotMap> {
    const [
      tasks,
      categories,
      priorities,
      dependencies,
      recurrence,
      comments,
      attachments,
      reminders,
      checklist,
    ] = await Promise.all([
      this.tasks.findAll(),
      this.categories.findAll(),
      this.priorities.findAll(),
      this.dependencies.findAll(),
      this.recurrence.findAll(),
      this.comments.findAll(),
      this.attachments.findAll(),
      this.reminders.findAll(),
      this.checklist.findAll(),
    ]);

    return {
      tasks: { items: tasks, version: this.snapshots.tasks.version },
      categories: { items: categories, version: this.snapshots.categories.version },
      priorities: { items: priorities, version: this.snapshots.priorities.version },
      dependencies: { items: dependencies, version: this.snapshots.dependencies.version },
      recurrence: { items: recurrence, version: this.snapshots.recurrence.version },
      comments: { items: comments, version: this.snapshots.comments.version },
      attachments: { items: attachments, version: this.snapshots.attachments.version },
      reminders: { items: reminders, version: this.snapshots.reminders.version },
      checklist: { items: checklist, version: this.snapshots.checklist.version },
    };
  }

  private refreshRepositories(): void {
    this.tasks = makeTasksRepository(this.snapshots.tasks.items);
    this.categories = makeCrud<ICategoryDto>(this.snapshots.categories.items);
    this.priorities = makeCrud<IPriorityDto>(this.snapshots.priorities.items);
    this.dependencies = makeCrud<IDependencyDto>(this.snapshots.dependencies.items);
    this.recurrence = makeCrud<IRecurrenceRuleDto>(this.snapshots.recurrence.items);
    this.comments = makeCrud<ICommentDto>(this.snapshots.comments.items);
    this.attachments = makeCrud<IAttachmentDto>(this.snapshots.attachments.items);
    this.reminders = makeCrud<IReminderDto>(this.snapshots.reminders.items);
    this.checklist = makeChecklistRepository(this.snapshots.checklist.items);
  }

  async commit(): Promise<IUnitOfWorkCommitResult> {
    try {
      const baseline = this.snapshots;
      const nextState = await this.currentState();

      applyCascadeDeletes(nextState, baseline);

      const validationErrors = validateState(nextState);
      if (validationErrors.length > 0) {
        await this.rollback();
        return { success: false, validationErrors };
      }

      const verifyVersion = <TDto>(collection: CollectionKey, expected: number): void => {
        const current = loadEnvelope<TDto>(collection as never);
        if (current.version !== expected) {
          throw new ConcurrencyError(collection as never, expected, current.version);
        }
      };

      verifyVersion<ITaskDto>("tasks", baseline.tasks.version);
      verifyVersion<ICategoryDto>("categories", baseline.categories.version);
      verifyVersion<IPriorityDto>("priorities", baseline.priorities.version);
      verifyVersion<IDependencyDto>("dependencies", baseline.dependencies.version);
      verifyVersion<IRecurrenceRuleDto>("recurrence", baseline.recurrence.version);
      verifyVersion<ICommentDto>("comments", baseline.comments.version);
      verifyVersion<IAttachmentDto>("attachments", baseline.attachments.version);
      verifyVersion<IReminderDto>("reminders", baseline.reminders.version);
      verifyVersion<IChecklistItemDto>("checklist", baseline.checklist.version);

      const changes = {
        tasks: computeChangeset(baseline.tasks.items, nextState.tasks.items, (dto) => dto.id),
        categories: computeChangeset(baseline.categories.items, nextState.categories.items, (dto) => dto.id),
        priorities: computeChangeset(baseline.priorities.items, nextState.priorities.items, (dto) => dto.id),
        dependencies: computeChangeset(baseline.dependencies.items, nextState.dependencies.items, (dto) => dto.id),
        recurrence: computeChangeset(baseline.recurrence.items, nextState.recurrence.items, (dto) => dto.id),
        comments: computeChangeset(baseline.comments.items, nextState.comments.items, (dto) => dto.id),
        attachments: computeChangeset(baseline.attachments.items, nextState.attachments.items, (dto) => dto.id),
        reminders: computeChangeset(baseline.reminders.items, nextState.reminders.items, (dto) => dto.id),
        checklist: computeChangeset(baseline.checklist.items, nextState.checklist.items, (dto) => dto.id),
      };

      const applySave = <TDto>(
        collection: CollectionKey,
        next: Snapshot<TDto>,
        previous: Snapshot<TDto>,
      ): void => {
        if (!hasChanges(changes[collection as keyof typeof changes] as Changeset<TDto>)) {
          return;
        }
        const saved = saveEnvelope<TDto>(collection as never, next.items, previous.version);
        (baseline as SnapshotMap)[collection] = {
          items: cloneArray(saved.items),
          version: saved.version,
        } as never;
      };

      applySave<ITaskDto>("tasks", nextState.tasks, baseline.tasks);
      applySave<ICategoryDto>("categories", nextState.categories, baseline.categories);
      applySave<IPriorityDto>("priorities", nextState.priorities, baseline.priorities);
      applySave<IDependencyDto>("dependencies", nextState.dependencies, baseline.dependencies);
      applySave<IRecurrenceRuleDto>("recurrence", nextState.recurrence, baseline.recurrence);
      applySave<ICommentDto>("comments", nextState.comments, baseline.comments);
      applySave<IAttachmentDto>("attachments", nextState.attachments, baseline.attachments);
      applySave<IReminderDto>("reminders", nextState.reminders, baseline.reminders);
      applySave<IChecklistItemDto>("checklist", nextState.checklist, baseline.checklist);

      this.snapshots = {
        tasks: { ...baseline.tasks, items: cloneArray(baseline.tasks.items) },
        categories: { ...baseline.categories, items: cloneArray(baseline.categories.items) },
        priorities: { ...baseline.priorities, items: cloneArray(baseline.priorities.items) },
        dependencies: { ...baseline.dependencies, items: cloneArray(baseline.dependencies.items) },
        recurrence: { ...baseline.recurrence, items: cloneArray(baseline.recurrence.items) },
        comments: { ...baseline.comments, items: cloneArray(baseline.comments.items) },
        attachments: { ...baseline.attachments, items: cloneArray(baseline.attachments.items) },
        reminders: { ...baseline.reminders, items: cloneArray(baseline.reminders.items) },
        checklist: { ...baseline.checklist, items: cloneArray(baseline.checklist.items) },
      };

      this.refreshRepositories();
      return { success: true };
    } catch (error) {
      if (error instanceof ConcurrencyError) {
        await this.rollback();
        return { success: false, concurrencyConflict: true };
      }
      await this.rollback();
      return { success: false, validationErrors: [String(error)] };
    }
  }

  async rollback(): Promise<void> {
    this.snapshots = {
      tasks: loadSnapshot<ITaskDto>("tasks"),
      categories: loadSnapshot<ICategoryDto>("categories"),
      priorities: loadSnapshot<IPriorityDto>("priorities"),
      dependencies: loadSnapshot<IDependencyDto>("dependencies"),
      recurrence: loadSnapshot<IRecurrenceRuleDto>("recurrence"),
      comments: loadSnapshot<ICommentDto>("comments"),
      attachments: loadSnapshot<IAttachmentDto>("attachments"),
      reminders: loadSnapshot<IReminderDto>("reminders"),
      checklist: loadSnapshot<IChecklistItemDto>("checklist"),
    };
    this.refreshRepositories();
  }
}

export const createUnitOfWork = (): IUnitOfWork => new UnitOfWork();
