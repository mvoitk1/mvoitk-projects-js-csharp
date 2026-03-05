import { loadEnvelope, saveEnvelope } from "./localStorageGateway";
import type {
  EntityId,
  IAttachmentsRepository,
  IAttachmentDto,
  ICommentsRepository,
  ICommentDto,
  IRemindersRepository,
  IReminderDto,
  ITaskDto,
} from "./types";

// What these next lines do:
// Small assert helper for readable validation errors.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const ensure = (condition: boolean, message: string): void => {
  if (!condition) {
    throw new Error(message);
  }
};

const cloneComment = (comment: ICommentDto): ICommentDto => ({ ...comment });
const cloneAttachment = (attachment: IAttachmentDto): IAttachmentDto => ({ ...attachment });
const cloneReminder = (reminder: IReminderDto): IReminderDto => ({ ...reminder });

// What these next lines do:
// Standard timestamp format used by this layer.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const nowIsoString = (): string => new Date().toISOString();

// What these next lines do:
// Load valid task IDs so child records cannot point to missing tasks.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const loadTaskIds = (): Set<EntityId> => {
  const tasks = loadEnvelope<ITaskDto>("tasks").items;
  return new Set(tasks.map((t) => t.id));
};

// What these next lines do:
// Validation for each child entity type.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const validateComment = (comment: ICommentDto, taskIds: Set<EntityId>): void => {
  ensure(!!comment.id, "comment.id is required");
  ensure(!!comment.taskId, "comment.taskId is required");
  ensure(!!comment.body, "comment.body is required");
  ensure(!!comment.createdAt, "comment.createdAt is required");
  ensure(taskIds.has(comment.taskId), `comment.taskId ${comment.taskId} does not exist`);
};

const validateAttachment = (attachment: IAttachmentDto, taskIds: Set<EntityId>): void => {
  ensure(!!attachment.id, "attachment.id is required");
  ensure(!!attachment.taskId, "attachment.taskId is required");
  ensure(!!attachment.url, "attachment.url is required");
  ensure(!!attachment.createdAt, "attachment.createdAt is required");
  ensure(taskIds.has(attachment.taskId), `attachment.taskId ${attachment.taskId} does not exist`);
};

const validateReminder = (reminder: IReminderDto, taskIds: Set<EntityId>): void => {
  ensure(!!reminder.id, "reminder.id is required");
  ensure(!!reminder.taskId, "reminder.taskId is required");
  ensure(!!reminder.remindAt, "reminder.remindAt is required");
  ensure(!!reminder.createdAt, "reminder.createdAt is required");
  ensure(taskIds.has(reminder.taskId), `reminder.taskId ${reminder.taskId} does not exist`);
};

// What these next lines do:
// CRUD repository for task comments.
// Why this matters in this project:
// Encapsulating this logic in a class keeps related state and behavior together, which makes the flow easier to maintain.
class CommentsRepository implements ICommentsRepository {
  private items: ICommentDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<ICommentDto>("comments");
    this.items = envelope.items.map(cloneComment);
    this.version = envelope.version;
  }

  // What these next lines do:
  // Persist collection with optimistic version check.
  // Why this matters in this project:
  // The version check avoids overwriting newer writes from another operation or tab.
  private async persist(next: ICommentDto[]): Promise<void> {
    const envelope = saveEnvelope<ICommentDto>("comments", next, this.version);
    this.items = envelope.items.map(cloneComment);
    this.version = envelope.version;
  }

  async findAll(): Promise<ICommentDto[]> {
    return this.items.map(cloneComment);
  }

  async findById(id: EntityId): Promise<ICommentDto | undefined> {
    const found = this.items.find((comment) => comment.id === id);
    return found ? cloneComment(found) : undefined;
  }

  async create(dto: ICommentDto): Promise<ICommentDto> {
    const taskIds = loadTaskIds();
    const candidate: ICommentDto = {
      ...dto,
      createdAt: dto.createdAt ?? nowIsoString(),
      updatedAt: dto.updatedAt ?? dto.createdAt ?? nowIsoString(),
    };
    validateComment(candidate, taskIds);
    ensure(!this.items.some((c) => c.id === candidate.id), `Entity with id ${candidate.id} already exists`);

    const next = [...this.items, candidate].map(cloneComment);
    await this.persist(next);
    return cloneComment(candidate);
  }

  async update(dto: ICommentDto): Promise<ICommentDto> {
    const existing = this.items.find((c) => c.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const taskIds = loadTaskIds();
    const candidate: ICommentDto = {
      ...existing!,
      ...dto,
      createdAt: existing?.createdAt ?? dto.createdAt ?? nowIsoString(),
      updatedAt: nowIsoString(),
    };
    validateComment(candidate, taskIds);

    const next = this.items.map((c) => (c.id === candidate.id ? candidate : c)).map(cloneComment);
    await this.persist(next);
    return cloneComment(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    const next = this.items.filter((c) => c.id !== id).map(cloneComment);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }
}

// What these next lines do:
// CRUD repository for attachments.
// Why this matters in this project:
// Encapsulating this logic in a class keeps related state and behavior together, which makes the flow easier to maintain.
class AttachmentsRepository implements IAttachmentsRepository {
  private items: IAttachmentDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<IAttachmentDto>("attachments");
    this.items = envelope.items.map(cloneAttachment);
    this.version = envelope.version;
  }

  // What these next lines do:
  // Persist collection with optimistic version check.
  // Why this matters in this project:
  // The version check avoids overwriting newer writes from another operation or tab.
  private async persist(next: IAttachmentDto[]): Promise<void> {
    const envelope = saveEnvelope<IAttachmentDto>("attachments", next, this.version);
    this.items = envelope.items.map(cloneAttachment);
    this.version = envelope.version;
  }

  async findAll(): Promise<IAttachmentDto[]> {
    return this.items.map(cloneAttachment);
  }

  async findById(id: EntityId): Promise<IAttachmentDto | undefined> {
    const found = this.items.find((attachment) => attachment.id === id);
    return found ? cloneAttachment(found) : undefined;
  }

  async create(dto: IAttachmentDto): Promise<IAttachmentDto> {
    const taskIds = loadTaskIds();
    const candidate: IAttachmentDto = {
      ...dto,
      createdAt: dto.createdAt ?? nowIsoString(),
    };
    validateAttachment(candidate, taskIds);
    ensure(!this.items.some((a) => a.id === candidate.id), `Entity with id ${candidate.id} already exists`);

    const next = [...this.items, candidate].map(cloneAttachment);
    await this.persist(next);
    return cloneAttachment(candidate);
  }

  async update(dto: IAttachmentDto): Promise<IAttachmentDto> {
    const existing = this.items.find((a) => a.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const taskIds = loadTaskIds();
    const candidate: IAttachmentDto = {
      ...existing!,
      ...dto,
      createdAt: existing?.createdAt ?? dto.createdAt ?? nowIsoString(),
    };
    validateAttachment(candidate, taskIds);

    const next = this.items.map((a) => (a.id === candidate.id ? candidate : a)).map(cloneAttachment);
    await this.persist(next);
    return cloneAttachment(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    const next = this.items.filter((a) => a.id !== id).map(cloneAttachment);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }
}

// What these next lines do:
// CRUD repository for reminders.
// Why this matters in this project:
// Encapsulating this logic in a class keeps related state and behavior together, which makes the flow easier to maintain.
class RemindersRepository implements IRemindersRepository {
  private items: IReminderDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<IReminderDto>("reminders");
    this.items = envelope.items.map(cloneReminder);
    this.version = envelope.version;
  }

  // What these next lines do:
  // Persist collection with optimistic version check.
  // Why this matters in this project:
  // The version check avoids overwriting newer writes from another operation or tab.
  private async persist(next: IReminderDto[]): Promise<void> {
    const envelope = saveEnvelope<IReminderDto>("reminders", next, this.version);
    this.items = envelope.items.map(cloneReminder);
    this.version = envelope.version;
  }

  async findAll(): Promise<IReminderDto[]> {
    return this.items.map(cloneReminder);
  }

  async findById(id: EntityId): Promise<IReminderDto | undefined> {
    const found = this.items.find((reminder) => reminder.id === id);
    return found ? cloneReminder(found) : undefined;
  }

  async create(dto: IReminderDto): Promise<IReminderDto> {
    const taskIds = loadTaskIds();
    const candidate: IReminderDto = {
      ...dto,
      createdAt: dto.createdAt ?? nowIsoString(),
    };
    validateReminder(candidate, taskIds);
    ensure(!this.items.some((r) => r.id === candidate.id), `Entity with id ${candidate.id} already exists`);

    const next = [...this.items, candidate].map(cloneReminder);
    await this.persist(next);
    return cloneReminder(candidate);
  }

  async update(dto: IReminderDto): Promise<IReminderDto> {
    const existing = this.items.find((r) => r.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const taskIds = loadTaskIds();
    const candidate: IReminderDto = {
      ...existing!,
      ...dto,
      createdAt: existing?.createdAt ?? dto.createdAt ?? nowIsoString(),
    };
    validateReminder(candidate, taskIds);

    const next = this.items.map((r) => (r.id === candidate.id ? candidate : r)).map(cloneReminder);
    await this.persist(next);
    return cloneReminder(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    const next = this.items.filter((r) => r.id !== id).map(cloneReminder);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }
}

export const createCommentsRepository = (): ICommentsRepository => new CommentsRepository();

export const createAttachmentsRepository = (): IAttachmentsRepository => new AttachmentsRepository();

export const createRemindersRepository = (): IRemindersRepository => new RemindersRepository();
