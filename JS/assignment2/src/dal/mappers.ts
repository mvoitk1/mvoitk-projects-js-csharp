import type {
  EntityId,
  IAttachmentDto,
  ICategoryDto,
  IChecklistItemDto,
  ICommentDto,
  IDependencyDto,
  IPriorityDto,
  IRecurrenceRuleDto,
  IReminderDto,
  ITaskDto,
} from "./types";

// What these next lines do:
// Domain aliases currently match DTOs, but are kept separate for future growth.
// Why this matters in this project:
// Exporting this type lets other modules share the same contract and keeps TypeScript checks consistent across the project.
export type ITask = ITaskDto;
export type ICategory = ICategoryDto;
export type IPriority = IPriorityDto;
export type IDependency = IDependencyDto;
export type IRecurrenceRule = IRecurrenceRuleDto;
export type IChecklistItem = IChecklistItemDto;
export type IComment = ICommentDto;
export type IAttachment = IAttachmentDto;
export type IReminder = IReminderDto;

// What these next lines do:
// Optional lookup sets used to validate foreign keys.
// Why this matters in this project:
// Validation here stops bad input early so broken data does not spread to storage or UI.
export interface IForeignKeyContext {
  taskIds?: Set<EntityId>;
  categoryIds?: Set<EntityId>;
  priorityIds?: Set<EntityId>;
  recurrenceRuleIds?: Set<EntityId>;
  dependencyIds?: Set<EntityId>;
  checklistItemIds?: Set<EntityId>;
  commentIds?: Set<EntityId>;
  attachmentIds?: Set<EntityId>;
  reminderIds?: Set<EntityId>;
}

type ValidationIssue = string;

// What these next lines do:
// Treat undefined/null/blank string as missing input.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const isMissing = (value: unknown): boolean =>
  value === undefined || value === null || (typeof value === "string" && value.trim() === "");

const requireField = (value: unknown, field: string, issues: ValidationIssue[]): void => {
  if (isMissing(value)) {
    issues.push(`${field} is required`);
  }
};

// What these next lines do:
// Validate one foreign key value against an allowed-id set.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const validateForeignKey = (
  id: EntityId | undefined,
  field: string,
  allowed: Set<EntityId> | undefined,
  issues: ValidationIssue[],
  required = true,
): void => {
  if (isMissing(id)) {
    if (required) {
      issues.push(`${field} is required`);
    }
    return;
  }

  const allowedSet = allowed ?? new Set<EntityId>();
  if (!allowedSet.has(id as EntityId)) {
    issues.push(`${field} ${String(id)} does not exist`);
  }
};

// What these next lines do:
// Validate every id in a list field (for arrays like dependencyIds).
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const validateForeignKeys = (
  ids: EntityId[] | undefined,
  field: string,
  allowed: Set<EntityId> | undefined,
  issues: ValidationIssue[],
): EntityId[] => {
  if (!ids) return [];
  ids.forEach((id) => validateForeignKey(id, field, allowed, issues, true));
  return ids;
};

// What these next lines do:
// Throw one clear error with all collected issues.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const ensureValid = (entity: string, issues: ValidationIssue[]): void => {
  if (issues.length > 0) {
    throw new Error(`${entity} validation failed: ${issues.join("; ")}`);
  }
};

// What these next lines do:
// Entity-specific validators.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const validateTaskShape = (
  task: ITaskDto,
  fk: IForeignKeyContext,
  issues: ValidationIssue[],
): void => {
  requireField(task.id, "task.id", issues);
  requireField(task.title, "task.title", issues);
  requireField(task.status, "task.status", issues);
  requireField(task.priorityId, "task.priorityId", issues);
  requireField(task.categoryId, "task.categoryId", issues);
  requireField(task.createdAt, "task.createdAt", issues);
  requireField(task.updatedAt, "task.updatedAt", issues);

  validateForeignKey(task.priorityId, "task.priorityId", fk.priorityIds, issues, true);
  validateForeignKey(task.categoryId, "task.categoryId", fk.categoryIds, issues, true);
  if (task.recurrenceRuleId) {
    validateForeignKey(
      task.recurrenceRuleId,
      "task.recurrenceRuleId",
      fk.recurrenceRuleIds,
      issues,
      false,
    );
  }

  validateForeignKeys(task.dependencyIds, "task.dependencyIds", fk.dependencyIds, issues);
  validateForeignKeys(task.checklistItemIds, "task.checklistItemIds", fk.checklistItemIds, issues);
  validateForeignKeys(task.commentIds, "task.commentIds", fk.commentIds, issues);
  validateForeignKeys(task.attachmentIds, "task.attachmentIds", fk.attachmentIds, issues);
  validateForeignKeys(task.reminderIds, "task.reminderIds", fk.reminderIds, issues);
};

const validateCategoryShape = (category: ICategoryDto, issues: ValidationIssue[]): void => {
  requireField(category.id, "category.id", issues);
  requireField(category.name, "category.name", issues);
};

const validatePriorityShape = (priority: IPriorityDto, issues: ValidationIssue[]): void => {
  requireField(priority.id, "priority.id", issues);
  requireField(priority.label, "priority.label", issues);
  requireField(priority.value, "priority.value", issues);
};

const validateDependencyShape = (
  dependency: IDependencyDto,
  fk: IForeignKeyContext,
  issues: ValidationIssue[],
): void => {
  requireField(dependency.id, "dependency.id", issues);
  requireField(dependency.taskId, "dependency.taskId", issues);
  requireField(dependency.dependsOnId, "dependency.dependsOnId", issues);
  requireField(dependency.type, "dependency.type", issues);

  validateForeignKey(dependency.taskId, "dependency.taskId", fk.taskIds, issues, true);
  validateForeignKey(
    dependency.dependsOnId,
    "dependency.dependsOnId",
    fk.taskIds,
    issues,
    true,
  );
};

const validateRecurrenceShape = (
  rule: IRecurrenceRuleDto,
  fk: IForeignKeyContext,
  issues: ValidationIssue[],
): void => {
  requireField(rule.id, "recurrenceRule.id", issues);
  requireField(rule.taskId, "recurrenceRule.taskId", issues);
  requireField(rule.frequency, "recurrenceRule.frequency", issues);

  validateForeignKey(rule.taskId, "recurrenceRule.taskId", fk.taskIds, issues, true);
};

const validateChecklistShape = (
  item: IChecklistItemDto,
  fk: IForeignKeyContext,
  issues: ValidationIssue[],
): void => {
  requireField(item.id, "checklistItem.id", issues);
  requireField(item.parentTaskId, "checklistItem.parentTaskId", issues);
  requireField(item.title, "checklistItem.title", issues);

  validateForeignKey(item.parentTaskId, "checklistItem.parentTaskId", fk.taskIds, issues, true);
};

const validateCommentShape = (
  comment: ICommentDto,
  fk: IForeignKeyContext,
  issues: ValidationIssue[],
): void => {
  requireField(comment.id, "comment.id", issues);
  requireField(comment.taskId, "comment.taskId", issues);
  requireField(comment.body, "comment.body", issues);
  requireField(comment.createdAt, "comment.createdAt", issues);

  validateForeignKey(comment.taskId, "comment.taskId", fk.taskIds, issues, true);
};

const validateAttachmentShape = (
  attachment: IAttachmentDto,
  fk: IForeignKeyContext,
  issues: ValidationIssue[],
): void => {
  requireField(attachment.id, "attachment.id", issues);
  requireField(attachment.taskId, "attachment.taskId", issues);
  requireField(attachment.url, "attachment.url", issues);
  requireField(attachment.createdAt, "attachment.createdAt", issues);

  validateForeignKey(attachment.taskId, "attachment.taskId", fk.taskIds, issues, true);
};

const validateReminderShape = (
  reminder: IReminderDto,
  fk: IForeignKeyContext,
  issues: ValidationIssue[],
): void => {
  requireField(reminder.id, "reminder.id", issues);
  requireField(reminder.taskId, "reminder.taskId", issues);
  requireField(reminder.remindAt, "reminder.remindAt", issues);
  requireField(reminder.createdAt, "reminder.createdAt", issues);

  validateForeignKey(reminder.taskId, "reminder.taskId", fk.taskIds, issues, true);
};

const withDefaults = (task: ITaskDto): ITaskDto => ({
  ...task,
  dependencyIds: task.dependencyIds ?? [],
  checklistItemIds: task.checklistItemIds ?? [],
  commentIds: task.commentIds ?? [],
  attachmentIds: task.attachmentIds ?? [],
  reminderIds: task.reminderIds ?? [],
  tagIds: task.tagIds ?? [],
  assigneeIds: task.assigneeIds ?? [],
});

// What these next lines do:
// Mapper functions below validate input, then return normalized objects.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
export const mapTaskDtoToDomain = (dto: ITaskDto, fk: IForeignKeyContext = {}): ITask => {
  const issues: ValidationIssue[] = [];
  validateTaskShape(dto, fk, issues);
  ensureValid("Task DTO", issues);
  return withDefaults(dto);
};

export const mapTaskDomainToDto = (task: ITask, fk: IForeignKeyContext = {}): ITaskDto => {
  const issues: ValidationIssue[] = [];
  validateTaskShape(task, fk, issues);
  ensureValid("Task domain", issues);
  return withDefaults(task);
};

export const mapCategoryDtoToDomain = (dto: ICategoryDto): ICategory => {
  const issues: ValidationIssue[] = [];
  validateCategoryShape(dto, issues);
  ensureValid("Category DTO", issues);
  return dto;
};

export const mapCategoryDomainToDto = (category: ICategory): ICategoryDto => {
  const issues: ValidationIssue[] = [];
  validateCategoryShape(category, issues);
  ensureValid("Category domain", issues);
  return category;
};

export const mapPriorityDtoToDomain = (dto: IPriorityDto): IPriority => {
  const issues: ValidationIssue[] = [];
  validatePriorityShape(dto, issues);
  ensureValid("Priority DTO", issues);
  return dto;
};

export const mapPriorityDomainToDto = (priority: IPriority): IPriorityDto => {
  const issues: ValidationIssue[] = [];
  validatePriorityShape(priority, issues);
  ensureValid("Priority domain", issues);
  return priority;
};

export const mapDependencyDtoToDomain = (
  dto: IDependencyDto,
  fk: IForeignKeyContext = {},
): IDependency => {
  const issues: ValidationIssue[] = [];
  validateDependencyShape(dto, fk, issues);
  ensureValid("Dependency DTO", issues);
  return dto;
};

export const mapDependencyDomainToDto = (
  dependency: IDependency,
  fk: IForeignKeyContext = {},
): IDependencyDto => {
  const issues: ValidationIssue[] = [];
  validateDependencyShape(dependency, fk, issues);
  ensureValid("Dependency domain", issues);
  return dependency;
};

export const mapRecurrenceRuleDtoToDomain = (
  dto: IRecurrenceRuleDto,
  fk: IForeignKeyContext = {},
): IRecurrenceRule => {
  const issues: ValidationIssue[] = [];
  validateRecurrenceShape(dto, fk, issues);
  ensureValid("RecurrenceRule DTO", issues);
  return dto;
};

export const mapRecurrenceRuleDomainToDto = (
  rule: IRecurrenceRule,
  fk: IForeignKeyContext = {},
): IRecurrenceRuleDto => {
  const issues: ValidationIssue[] = [];
  validateRecurrenceShape(rule, fk, issues);
  ensureValid("RecurrenceRule domain", issues);
  return rule;
};

export const mapChecklistItemDtoToDomain = (
  dto: IChecklistItemDto,
  fk: IForeignKeyContext = {},
): IChecklistItem => {
  const issues: ValidationIssue[] = [];
  validateChecklistShape(dto, fk, issues);
  ensureValid("ChecklistItem DTO", issues);
  return dto;
};

export const mapChecklistItemDomainToDto = (
  item: IChecklistItem,
  fk: IForeignKeyContext = {},
): IChecklistItemDto => {
  const issues: ValidationIssue[] = [];
  validateChecklistShape(item, fk, issues);
  ensureValid("ChecklistItem domain", issues);
  return item;
};

export const mapCommentDtoToDomain = (
  dto: ICommentDto,
  fk: IForeignKeyContext = {},
): IComment => {
  const issues: ValidationIssue[] = [];
  validateCommentShape(dto, fk, issues);
  ensureValid("Comment DTO", issues);
  return dto;
};

export const mapCommentDomainToDto = (
  comment: IComment,
  fk: IForeignKeyContext = {},
): ICommentDto => {
  const issues: ValidationIssue[] = [];
  validateCommentShape(comment, fk, issues);
  ensureValid("Comment domain", issues);
  return comment;
};

export const mapAttachmentDtoToDomain = (
  dto: IAttachmentDto,
  fk: IForeignKeyContext = {},
): IAttachment => {
  const issues: ValidationIssue[] = [];
  validateAttachmentShape(dto, fk, issues);
  ensureValid("Attachment DTO", issues);
  return dto;
};

export const mapAttachmentDomainToDto = (
  attachment: IAttachment,
  fk: IForeignKeyContext = {},
): IAttachmentDto => {
  const issues: ValidationIssue[] = [];
  validateAttachmentShape(attachment, fk, issues);
  ensureValid("Attachment domain", issues);
  return attachment;
};

export const mapReminderDtoToDomain = (
  dto: IReminderDto,
  fk: IForeignKeyContext = {},
): IReminder => {
  const issues: ValidationIssue[] = [];
  validateReminderShape(dto, fk, issues);
  ensureValid("Reminder DTO", issues);
  return dto;
};

export const mapReminderDomainToDto = (
  reminder: IReminder,
  fk: IForeignKeyContext = {},
): IReminderDto => {
  const issues: ValidationIssue[] = [];
  validateReminderShape(reminder, fk, issues);
  ensureValid("Reminder domain", issues);
  return reminder;
};
