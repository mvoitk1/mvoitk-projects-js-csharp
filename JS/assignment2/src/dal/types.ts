// What these next lines do:
// Every object ID in this app is just text (for example: "task-123").
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type EntityId = string;

// What these next lines do:
// Allowed task status values. Using this prevents typos in status strings.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const ETaskStatus = {
  Todo: "todo",
  InProgress: "in-progress",
  Blocked: "blocked",
  Completed: "completed",
  Cancelled: "cancelled",
} as const;
// What these next lines do:
// Type made from the values above ("todo" | "in-progress" | ...).
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type ETaskStatus = (typeof ETaskStatus)[keyof typeof ETaskStatus];

// What these next lines do:
// Allowed task priority values.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const ETaskPriority = {
  Low: "low",
  Medium: "medium",
  High: "high",
  Critical: "critical",
} as const;
export type ETaskPriority = (typeof ETaskPriority)[keyof typeof ETaskPriority];

// What these next lines do:
// Allowed relationship types between tasks.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const EDependencyType = {
  Blocks: "blocks",
  BlockedBy: "blocked-by",
  RelatesTo: "relates-to",
  Duplicates: "duplicates",
} as const;
export type EDependencyType =
  (typeof EDependencyType)[keyof typeof EDependencyType];

// What these next lines do:
// Allowed repeat schedules for recurring tasks.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const ERecurrenceFrequency = {
  Daily: "daily",
  Weekly: "weekly",
  Monthly: "monthly",
  Yearly: "yearly",
} as const;
export type ERecurrenceFrequency =
  (typeof ERecurrenceFrequency)[keyof typeof ERecurrenceFrequency];

// What these next lines do:
// Allowed reminder delivery channels.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const EReminderType = {
  InApp: "in-app",
  Email: "email",
  Push: "push",
} as const;
export type EReminderType = (typeof EReminderType)[keyof typeof EReminderType];

// What these next lines do:
// Allowed fields users can sort tasks by.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const ESortField = {
  Priority: "priority",
  Status: "status",
  DueDate: "dueDate",
  CreatedAt: "createdAt",
  UpdatedAt: "updatedAt",
  CompletedAt: "completedAt",
  Title: "title",
} as const;
export type ESortField = (typeof ESortField)[keyof typeof ESortField];

// What these next lines do:
// Sort direction options.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const ESortDirection = {
  Asc: "asc",
  Desc: "desc",
} as const;
export type ESortDirection =
  (typeof ESortDirection)[keyof typeof ESortDirection];

// What these next lines do:
// Rules for filtering task lists (search text, dates, tags, assignees, etc.).
// Why this matters in this project:
// Applying this filter here ensures users only see tasks matching the chosen criteria.
export interface ITaskFilter {
  query?: string;
  text?: string;
  status?: ETaskStatus[];
  priorityIds?: EntityId[];
  categoryIds?: EntityId[];
  projectIds?: EntityId[];
  boardIds?: EntityId[];
  listIds?: EntityId[];
  tagIds?: EntityId[];
  assigneeIds?: EntityId[];
  dueBefore?: string;
  dueAfter?: string;
  completedBefore?: string;
  completedAfter?: string;
  createdBefore?: string;
  createdAfter?: string;
  updatedBefore?: string;
  updatedAfter?: string;
  hasRecurrence?: boolean;
  recurrenceId?: EntityId;
  blocked?: boolean;
}

// What these next lines do:
// Describes one "sort by ..." instruction.
// Why this matters in this project:
// Sorting at this step guarantees a consistent order in UI views and command outputs.
export interface ITaskSortDescriptor {
  field: ESortField;
  direction?: ESortDirection;
  comparator?: (a: ITaskDto, b: ITaskDto) => number;
}

// What these next lines do:
// Summary numbers shown in dashboards or reports.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Summary numbers shown in dashboards or reports.
export interface ITaskStats {
  total: number;
  byStatus: Record<ETaskStatus, number>;
  byPriority: Record<EntityId, number>;
  byCategory: Record<EntityId, number>;
  overdue: number;
  upcoming: number;
  blocked: number;
}

// What these next lines do:
// Standard "who/when changed this" metadata.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Standard "who/when changed this" metadata.
export interface IAuditMetadata {
  createdAt: string;
  updatedAt: string;
  createdBy?: EntityId;
  updatedBy?: EntityId;
}

// What these next lines do:
// Category shape as stored/transferred (DTO = Data Transfer Object).
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Category shape as stored/transferred (DTO = Data Transfer Object).
export interface ICategoryDto {
  id: EntityId;
  name: string;
  description?: string;
  color?: string;
  order?: number;
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Priority shape as stored/transferred.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Priority shape as stored/transferred.
export interface IPriorityDto {
  id: EntityId;
  label: string;
  value: ETaskPriority;
  description?: string;
  color?: string;
  order?: number;
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Checklist item that belongs to a task.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Checklist item that belongs to a task.
export interface IChecklistItemDto {
  id: EntityId;
  parentTaskId: EntityId;
  title: string;
  completed: boolean;
  completedAt?: string;
  assigneeId?: EntityId;
  subTaskId?: EntityId;
  createdAt?: string;
  updatedAt?: string;
}

// What these next lines do:
// Comment attached to a task.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Comment attached to a task.
export interface ICommentDto {
  id: EntityId;
  taskId: EntityId;
  authorId?: EntityId;
  body: string;
  createdAt: string;
  updatedAt?: string;
}

// What these next lines do:
// File/link attached to a task.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: File/link attached to a task.
export interface IAttachmentDto {
  id: EntityId;
  taskId: EntityId;
  url: string;
  name?: string;
  mimeType?: string;
  sizeBytes?: number;
  createdAt: string;
}

// What these next lines do:
// Reminder for a task.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Reminder for a task.
export interface IReminderDto {
  id: EntityId;
  taskId: EntityId;
  remindAt: string;
  channel?: EReminderType;
  createdAt: string;
  completedAt?: string;
}

// What these next lines do:
// Full recurrence rule (how and when a task repeats).
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Full recurrence rule (how and when a task repeats).
export interface IRecurrenceRuleDto {
  id: EntityId;
  taskId: EntityId;
  frequency: ERecurrenceFrequency;
  interval?: number;
  count?: number;
  until?: string;
  startDate?: string;
  timeOfDay?: string;
  byDay?: string[];
  byMonth?: number[];
  byMonthDay?: number[];
  byYearDay?: number[];
  weekStart?: string;
  timezone?: string;
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Dependency between two tasks (one depends on the other).
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Dependency between two tasks (one depends on the other).
export interface IDependencyDto {
  id: EntityId;
  taskId: EntityId;
  dependsOnId: EntityId;
  type: EDependencyType;
  note?: string;
  createdBy?: EntityId;
  createdAt?: string;
}

// What these next lines do:
// Main task record as stored/transferred.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Main task record as stored/transferred.
export interface ITaskDto {
  id: EntityId;
  title: string;
  description?: string;
  status: ETaskStatus;
  priorityId: EntityId;
  categoryId: EntityId;
  projectId?: EntityId;
  boardId?: EntityId;
  listId?: EntityId;
  dueDate?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
  recurrenceRuleId?: EntityId;
  recurrenceId?: EntityId;
  dependencyIds?: EntityId[];
  checklistItemIds?: EntityId[];
  commentIds?: EntityId[];
  attachmentIds?: EntityId[];
  reminderIds?: EntityId[];
  tagIds?: EntityId[];
  assigneeIds?: EntityId[];
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Domain model category, extended with reverse links to tasks.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Domain model category, extended with reverse links to tasks.
export interface ICategory extends ICategoryDto {
  taskIds?: EntityId[];
}

// What these next lines do:
// Domain model priority, extended with reverse links to tasks.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Domain model priority, extended with reverse links to tasks.
export interface IPriority extends IPriorityDto {
  taskIds?: EntityId[];
}

// What these next lines do:
// Project groups boards/lists/tasks.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Project groups boards/lists/tasks.
export interface IProject {
  id: EntityId;
  name: string;
  description?: string;
  color?: string;
  order?: number;
  boardIds?: EntityId[];
  listIds?: EntityId[];
  taskIds?: EntityId[];
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Board usually represents a workflow view inside a project.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Board usually represents a workflow view inside a project.
export interface IBoard {
  id: EntityId;
  name: string;
  projectId?: EntityId;
  description?: string;
  order?: number;
  listIds?: EntityId[];
  taskIds?: EntityId[];
  metadata?: IAuditMetadata;
}

// What these next lines do:
// List is another grouping level for tasks.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: List is another grouping level for tasks.
export interface IList {
  id: EntityId;
  name: string;
  projectId?: EntityId;
  boardId?: EntityId;
  description?: string;
  order?: number;
  taskIds?: EntityId[];
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Tag is a label you can attach to many tasks.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Tag is a label you can attach to many tasks.
export interface ITag {
  id: EntityId;
  name: string;
  color?: string;
  description?: string;
  taskIds?: EntityId[];
  metadata?: IAuditMetadata;
}

// What these next lines do:
// User who can be assigned to tasks.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: User who can be assigned to tasks.
export interface IUser {
  id: EntityId;
  displayName: string;
  email?: string;
  avatarUrl?: string;
  handle?: string;
  assignedTaskIds?: EntityId[];
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Domain aliases of DTOs; kept separate so domain types can diverge later.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Domain aliases of DTOs; kept separate so domain types can diverge later.
export interface IComment extends ICommentDto {}

export interface IAttachment extends IAttachmentDto {}

export interface IReminder extends IReminderDto {}

export interface IChecklistItem extends IChecklistItemDto {}

// What these next lines do:
// Domain recurrence rule adds optional recurrence group ID.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Domain recurrence rule adds optional recurrence group ID.
export interface IRecurrenceRule extends IRecurrenceRuleDto {
  recurrenceId?: EntityId;
}

// What these next lines do:
// Domain dependency can also carry metadata.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Domain dependency can also carry metadata.
export interface IDependency extends IDependencyDto {
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Rich task model with embedded related entities.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Rich task model with embedded related entities.
export interface ITask extends ITaskDto {
  recurrenceRule?: IRecurrenceRule;
  dependencies?: IDependency[];
  checklist?: IChecklistItem[];
  comments?: IComment[];
  attachments?: IAttachment[];
  reminders?: IReminder[];
  assigneeIds?: EntityId[];
  tagIds?: EntityId[];
  metadata?: IAuditMetadata;
}

// What these next lines do:
// Collection fields on tasks that may need normalization/defaulting.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type ITaskCollectionKeys =
  | "tagIds"
  | "assigneeIds"
  | "dependencyIds"
  | "checklistItemIds"
  | "commentIds"
  | "attachmentIds"
  | "reminderIds";

// What these next lines do:
// Default values used when old/incomplete task data is repaired.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Default values used when old/incomplete task data is repaired.
export interface ITaskDefaultingPlan {
  fallbackStatus: ETaskStatus;
  fallbackPriorityId: EntityId;
  fallbackCategoryId: EntityId;
  fallbackProjectId?: EntityId;
  fallbackBoardId?: EntityId;
  fallbackListId?: EntityId;
  defaultDueDate?: string | null;
  defaultRecurrenceRule?: IRecurrenceRule | null;
  defaultDependencies?: IDependency[];
  defaultReminders?: IReminder[];
  normalizeCollections?: ITaskCollectionKeys[];
}

// What these next lines do:
// Context for coercing old data into current valid data.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Context for coercing old data into current valid data.
export interface ILegacyCoercionContext {
  now: string;
  plan: ITaskDefaultingPlan;
}

// What these next lines do:
// Result of coercion: value plus non-fatal warning messages.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Result of coercion: value plus non-fatal warning messages.
export interface ICoercionOutcome<T> {
  value: T;
  warnings: string[];
}

// What these next lines do:
// Minimal old task shape accepted by migration logic.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type LegacyTaskRecord = Partial<ITaskDto> & Pick<ITaskDto, "id" | "title">;

// What these next lines do:
// Function contract for converting one legacy task record.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type CoerceLegacyTaskFn = (
  record: LegacyTaskRecord,
  ctx: ILegacyCoercionContext,
) => ICoercionOutcome<ITaskDto>;

// What these next lines do:
// Function contract for converting a whole legacy task envelope.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type CoerceLegacyStorageFn = (
  legacyEnvelope: unknown,
  ctx: ILegacyCoercionContext,
) => ICoercionOutcome<IStorageEnvelope<ITaskDto>>;

// What these next lines do:
// Generic comparator function used by sorting utilities.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type SortComparator<TItem> = (a: TItem, b: TItem) => number;

// What these next lines do:
// Generic sort descriptor reusable beyond tasks.
// Why this matters in this project:
// Sorting at this step guarantees a consistent order in UI views and command outputs.
export interface ISortDescriptor<TField extends string = ESortField, TItem = ITaskDto> {
  field: TField;
  direction?: ESortDirection;
  comparator?: SortComparator<TItem>;
}

// What these next lines do:
// Optional runtime values used by matchers (like "current time").
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Optional runtime values used by matchers (like "current time").
export interface ITaskMatcherOptions {
  now?: string;
  timezone?: string;
}

// What these next lines do:
// Function contract: decides if a task passes a filter.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type TaskFilterMatcher = (
  task: ITask,
  filter?: ITaskFilter,
  options?: ITaskMatcherOptions,
) => boolean;

// What these next lines do:
// Function contract: search query + optional filter.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type TaskSearchMatcher = (
  task: ITask,
  query: string,
  filter?: ITaskFilter,
  options?: ITaskMatcherOptions,
) => boolean;

// What these next lines do:
// Options for generating recurrence dates.
// Why this matters in this project:
// Standardizing date handling avoids subtle bugs when comparing or displaying time values.
export interface IRecurrenceExpansionOptions {
  start?: string;
  end?: string;
  maxOccurrences?: number;
  timezone?: string;
  includeStart?: boolean;
}

// What these next lines do:
// Function contract: expand a recurrence rule into occurrence timestamps.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type RecurrenceExpansionFn = (
  rule: IRecurrenceRule,
  options?: IRecurrenceExpansionOptions,
) => string[];

// What these next lines do:
// Function contract: detect circular dependencies.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type DependencyCycleDetectionFn = (
  taskId: EntityId,
  dependencies: IDependency[],
) => {
  hasCycle: boolean;
  cycles: EntityId[][];
};

// What these next lines do:
// Function contract: calculate task statistics for a snapshot in time.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type TaskStatsAggregator = (
  tasks: ITask[],
  now: string,
  upcomingWindowDays?: number,
) => ITaskStats;

// What these next lines do:
// All top-level collections saved in localStorage.
// Why this matters in this project:
// Being explicit about storage behavior prevents corruption and makes recovery paths clearer.
export interface ILocalStorageCollections {
  tasks: IStorageEnvelope<ITaskDto>;
  categories: IStorageEnvelope<ICategoryDto>;
  priorities: IStorageEnvelope<IPriorityDto>;
  dependencies: IStorageEnvelope<IDependencyDto>;
  recurrenceRules: IStorageEnvelope<IRecurrenceRuleDto>;
  comments: IStorageEnvelope<ICommentDto>;
  attachments: IStorageEnvelope<IAttachmentDto>;
  reminders: IStorageEnvelope<IReminderDto>;
  checklist: IStorageEnvelope<IChecklistItemDto>;
  tags?: IStorageEnvelope<ITag>;
  users?: IStorageEnvelope<IUser>;
  boards?: IStorageEnvelope<IBoard>;
  lists?: IStorageEnvelope<IList>;
  projects?: IStorageEnvelope<IProject>;
}

// What these next lines do:
// Root localStorage document with schema info.
// Why this matters in this project:
// Being explicit about storage behavior prevents corruption and makes recovery paths clearer.
export interface ILocalStorageSchema {
  schemaVersion: number;
  updatedAt: string;
  collections: ILocalStorageCollections;
}

// What these next lines do:
// Standard wrapper around each stored collection.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Standard wrapper around each stored collection.
export interface IStorageEnvelope<TDto> {
  version: number;
  updatedAt: string;
  items: TDto[];
}

// What these next lines do:
// Generic CRUD repository contract.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Generic CRUD repository contract.
export interface ICrudRepository<TDto> {
  findAll(): Promise<TDto[]>;
  findById(id: EntityId): Promise<TDto | undefined>;
  create(dto: TDto): Promise<TDto>;
  update(dto: TDto): Promise<TDto>;
  delete(id: EntityId): Promise<void>;
}

// What these next lines do:
// Task-specific repository adds query and stats features.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Task-specific repository adds query and stats features.
export interface ITasksRepository extends ICrudRepository<ITaskDto> {
  findByCategory(categoryId: EntityId): Promise<ITaskDto[]>;
  findByPriority(priorityId: EntityId): Promise<ITaskDto[]>;
  query(filter?: ITaskFilter, sorts?: ITaskSortDescriptor[]): Promise<ITaskDto[]>;
  stats(now: string, upcomingWindowDays?: number): Promise<ITaskStats>;
}

// What these next lines do:
// Repository interfaces for the other entity types.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Repository interfaces for the other entity types.
export interface ICategoriesRepository extends ICrudRepository<ICategoryDto> {}

export interface IPrioritiesRepository extends ICrudRepository<IPriorityDto> {}

export interface IDependenciesRepository extends ICrudRepository<IDependencyDto> {}

export interface IRecurrenceRepository extends ICrudRepository<IRecurrenceRuleDto> {}

export interface ICommentsRepository extends ICrudRepository<ICommentDto> {}

export interface IAttachmentsRepository extends ICrudRepository<IAttachmentDto> {}

export interface IRemindersRepository extends ICrudRepository<IReminderDto> {}

export interface IChecklistRepository extends ICrudRepository<IChecklistItemDto> {}

// What these next lines do:
// Unit of Work groups repositories and gives commit/rollback boundaries.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Unit of Work groups repositories and gives commit/rollback boundaries.
export interface IUnitOfWork {
  tasks: ITasksRepository;
  categories: ICategoriesRepository;
  priorities: IPrioritiesRepository;
  dependencies: IDependenciesRepository;
  recurrence: IRecurrenceRepository;
  comments: ICommentsRepository;
  attachments: IAttachmentsRepository;
  reminders: IRemindersRepository;
  checklist: IChecklistRepository;
  commit(): Promise<IUnitOfWorkCommitResult>;
  rollback(): Promise<void>;
}

// What these next lines do:
// Result returned from a commit attempt.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Result returned from a commit attempt.
export interface IUnitOfWorkCommitResult {
  success: boolean;
  concurrencyConflict?: boolean;
  validationErrors?: string[];
}
