export type EntityId = string;

export const ETaskStatus = {
  Todo: "todo",
  InProgress: "in-progress",
  Blocked: "blocked",
  Completed: "completed",
  Cancelled: "cancelled",
} as const;
export type ETaskStatus = (typeof ETaskStatus)[keyof typeof ETaskStatus];

export const ETaskPriority = {
  Low: "low",
  Medium: "medium",
  High: "high",
  Critical: "critical",
} as const;
export type ETaskPriority = (typeof ETaskPriority)[keyof typeof ETaskPriority];

export const EDependencyType = {
  Blocks: "blocks",
  BlockedBy: "blocked-by",
  RelatesTo: "relates-to",
  Duplicates: "duplicates",
} as const;
export type EDependencyType =
  (typeof EDependencyType)[keyof typeof EDependencyType];

export const ERecurrenceFrequency = {
  Daily: "daily",
  Weekly: "weekly",
  Monthly: "monthly",
  Yearly: "yearly",
} as const;
export type ERecurrenceFrequency =
  (typeof ERecurrenceFrequency)[keyof typeof ERecurrenceFrequency];

export const EReminderType = {
  InApp: "in-app",
  Email: "email",
  Push: "push",
} as const;
export type EReminderType = (typeof EReminderType)[keyof typeof EReminderType];

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

export const ESortDirection = {
  Asc: "asc",
  Desc: "desc",
} as const;
export type ESortDirection =
  (typeof ESortDirection)[keyof typeof ESortDirection];

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

export interface ITaskSortDescriptor {
  field: ESortField;
  direction?: ESortDirection;
  comparator?: (a: ITaskDto, b: ITaskDto) => number;
}

export interface ITaskStats {
  total: number;
  byStatus: Record<ETaskStatus, number>;
  byPriority: Record<EntityId, number>;
  byCategory: Record<EntityId, number>;
  overdue: number;
  upcoming: number;
  blocked: number;
}

export interface IAuditMetadata {
  createdAt: string;
  updatedAt: string;
  createdBy?: EntityId;
  updatedBy?: EntityId;
}

export interface ICategoryDto {
  id: EntityId;
  name: string;
  description?: string;
  color?: string;
  order?: number;
  metadata?: IAuditMetadata;
}

export interface IPriorityDto {
  id: EntityId;
  label: string;
  value: ETaskPriority;
  description?: string;
  color?: string;
  order?: number;
  metadata?: IAuditMetadata;
}

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

export interface ICommentDto {
  id: EntityId;
  taskId: EntityId;
  authorId?: EntityId;
  body: string;
  createdAt: string;
  updatedAt?: string;
}

export interface IAttachmentDto {
  id: EntityId;
  taskId: EntityId;
  url: string;
  name?: string;
  mimeType?: string;
  sizeBytes?: number;
  createdAt: string;
}

export interface IReminderDto {
  id: EntityId;
  taskId: EntityId;
  remindAt: string;
  channel?: EReminderType;
  createdAt: string;
  completedAt?: string;
}

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

export interface IDependencyDto {
  id: EntityId;
  taskId: EntityId;
  dependsOnId: EntityId;
  type: EDependencyType;
  note?: string;
  createdBy?: EntityId;
  createdAt?: string;
}

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

export interface ICategory extends ICategoryDto {
  taskIds?: EntityId[];
}

export interface IPriority extends IPriorityDto {
  taskIds?: EntityId[];
}

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

export interface ITag {
  id: EntityId;
  name: string;
  color?: string;
  description?: string;
  taskIds?: EntityId[];
  metadata?: IAuditMetadata;
}

export interface IUser {
  id: EntityId;
  displayName: string;
  email?: string;
  avatarUrl?: string;
  handle?: string;
  assignedTaskIds?: EntityId[];
  metadata?: IAuditMetadata;
}

export interface IComment extends ICommentDto {}

export interface IAttachment extends IAttachmentDto {}

export interface IReminder extends IReminderDto {}

export interface IChecklistItem extends IChecklistItemDto {}

export interface IRecurrenceRule extends IRecurrenceRuleDto {
  recurrenceId?: EntityId;
}

export interface IDependency extends IDependencyDto {
  metadata?: IAuditMetadata;
}

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

export type ITaskCollectionKeys =
  | "tagIds"
  | "assigneeIds"
  | "dependencyIds"
  | "checklistItemIds"
  | "commentIds"
  | "attachmentIds"
  | "reminderIds";

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

export interface ILegacyCoercionContext {
  now: string;
  plan: ITaskDefaultingPlan;
}

export interface ICoercionOutcome<T> {
  value: T;
  warnings: string[];
}

export type LegacyTaskRecord = Partial<ITaskDto> & Pick<ITaskDto, "id" | "title">;

export type CoerceLegacyTaskFn = (
  record: LegacyTaskRecord,
  ctx: ILegacyCoercionContext,
) => ICoercionOutcome<ITaskDto>;

export type CoerceLegacyStorageFn = (
  legacyEnvelope: unknown,
  ctx: ILegacyCoercionContext,
) => ICoercionOutcome<IStorageEnvelope<ITaskDto>>;

export type SortComparator<TItem> = (a: TItem, b: TItem) => number;

export interface ISortDescriptor<TField extends string = ESortField, TItem = ITaskDto> {
  field: TField;
  direction?: ESortDirection;
  comparator?: SortComparator<TItem>;
}

export interface ITaskMatcherOptions {
  now?: string;
  timezone?: string;
}

export type TaskFilterMatcher = (
  task: ITask,
  filter?: ITaskFilter,
  options?: ITaskMatcherOptions,
) => boolean;

export type TaskSearchMatcher = (
  task: ITask,
  query: string,
  filter?: ITaskFilter,
  options?: ITaskMatcherOptions,
) => boolean;

export interface IRecurrenceExpansionOptions {
  start?: string;
  end?: string;
  maxOccurrences?: number;
  timezone?: string;
  includeStart?: boolean;
}

export type RecurrenceExpansionFn = (
  rule: IRecurrenceRule,
  options?: IRecurrenceExpansionOptions,
) => string[];

export type DependencyCycleDetectionFn = (
  taskId: EntityId,
  dependencies: IDependency[],
) => {
  hasCycle: boolean;
  cycles: EntityId[][];
};

export type TaskStatsAggregator = (
  tasks: ITask[],
  now: string,
  upcomingWindowDays?: number,
) => ITaskStats;

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

export interface ILocalStorageSchema {
  schemaVersion: number;
  updatedAt: string;
  collections: ILocalStorageCollections;
}

export interface IStorageEnvelope<TDto> {
  version: number;
  updatedAt: string;
  items: TDto[];
}

export interface ICrudRepository<TDto> {
  findAll(): Promise<TDto[]>;
  findById(id: EntityId): Promise<TDto | undefined>;
  create(dto: TDto): Promise<TDto>;
  update(dto: TDto): Promise<TDto>;
  delete(id: EntityId): Promise<void>;
}

export interface ITasksRepository extends ICrudRepository<ITaskDto> {
  findByCategory(categoryId: EntityId): Promise<ITaskDto[]>;
  findByPriority(priorityId: EntityId): Promise<ITaskDto[]>;
  query(filter?: ITaskFilter, sorts?: ITaskSortDescriptor[]): Promise<ITaskDto[]>;
  stats(now: string, upcomingWindowDays?: number): Promise<ITaskStats>;
}

export interface ICategoriesRepository extends ICrudRepository<ICategoryDto> {}

export interface IPrioritiesRepository extends ICrudRepository<IPriorityDto> {}

export interface IDependenciesRepository extends ICrudRepository<IDependencyDto> {}

export interface IRecurrenceRepository extends ICrudRepository<IRecurrenceRuleDto> {}

export interface ICommentsRepository extends ICrudRepository<ICommentDto> {}

export interface IAttachmentsRepository extends ICrudRepository<IAttachmentDto> {}

export interface IRemindersRepository extends ICrudRepository<IReminderDto> {}

export interface IChecklistRepository extends ICrudRepository<IChecklistItemDto> {}

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

export interface IUnitOfWorkCommitResult {
  success: boolean;
  concurrencyConflict?: boolean;
  validationErrors?: string[];
}
