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
  status?: ETaskStatus[];
  priorityIds?: EntityId[];
  categoryIds?: EntityId[];
  tagIds?: EntityId[];
  assigneeIds?: EntityId[];
  dueBefore?: string;
  dueAfter?: string;
  createdBefore?: string;
  createdAfter?: string;
  updatedBefore?: string;
  updatedAfter?: string;
  hasRecurrence?: boolean;
  blocked?: boolean;
}

export interface ITaskSortDescriptor {
  field: ESortField;
  direction?: ESortDirection;
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
  channel?: "in-app" | "email" | "push";
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
  dependencyIds?: EntityId[];
  checklistItemIds?: EntityId[];
  commentIds?: EntityId[];
  attachmentIds?: EntityId[];
  reminderIds?: EntityId[];
  tagIds?: EntityId[];
  assigneeIds?: EntityId[];
  metadata?: IAuditMetadata;
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
