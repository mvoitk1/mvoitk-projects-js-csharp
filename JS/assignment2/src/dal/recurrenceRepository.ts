import { loadEnvelope, saveEnvelope } from "./localStorageGateway";
import type {
  EntityId,
  IRecurrenceRepository,
  IRecurrenceRuleDto,
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

const cloneRule = (rule: IRecurrenceRuleDto): IRecurrenceRuleDto => ({ ...rule });

// What these next lines do:
// Standard timestamp format used by this layer.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const nowIsoString = (): string => new Date().toISOString();

// What these next lines do:
// Load valid task IDs so recurrence rules only target existing tasks.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const loadTaskIds = (): Set<EntityId> => {
  const tasks = loadEnvelope<ITaskDto>("tasks").items;
  return new Set(tasks.map((t) => t.id));
};

// What these next lines do:
// Basic recurrence rule validation.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const validateRule = (rule: IRecurrenceRuleDto, taskIds: Set<EntityId>): void => {
  ensure(!!rule.id, "recurrenceRule.id is required");
  ensure(!!rule.taskId, "recurrenceRule.taskId is required");
  ensure(!!rule.frequency, "recurrenceRule.frequency is required");
  ensure(taskIds.has(rule.taskId), `recurrenceRule.taskId ${rule.taskId} does not exist`);
};

// What these next lines do:
// Prevent deleting rules that tasks still point to.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const isRuleReferencedByTasks = (ruleId: EntityId): boolean => {
  const tasks = loadEnvelope<ITaskDto>("tasks").items;
  return tasks.some((task) => task.recurrenceRuleId === ruleId);
};

// What these next lines do:
// CRUD repository for recurrence rules.
// Why this matters in this project:
// Encapsulating this logic in a class keeps related state and behavior together, which makes the flow easier to maintain.
class RecurrenceRepository implements IRecurrenceRepository {
  private items: IRecurrenceRuleDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<IRecurrenceRuleDto>("recurrence");
    this.items = envelope.items.map(cloneRule);
    this.version = envelope.version;
  }

  // What these next lines do:
  // Persist collection with optimistic version check.
  // Why this matters in this project:
  // The version check avoids overwriting newer writes from another operation or tab.
  private async persist(next: IRecurrenceRuleDto[]): Promise<void> {
    const envelope = saveEnvelope<IRecurrenceRuleDto>("recurrence", next, this.version);
    this.items = envelope.items.map(cloneRule);
    this.version = envelope.version;
  }

  async findAll(): Promise<IRecurrenceRuleDto[]> {
    return this.items.map(cloneRule);
  }

  async findById(id: EntityId): Promise<IRecurrenceRuleDto | undefined> {
    const found = this.items.find((rule) => rule.id === id);
    return found ? cloneRule(found) : undefined;
  }

  async create(dto: IRecurrenceRuleDto): Promise<IRecurrenceRuleDto> {
    const taskIds = loadTaskIds();
    const candidate: IRecurrenceRuleDto = {
      ...dto,
      metadata: {
        ...dto.metadata,
        createdAt: dto.metadata?.createdAt ?? nowIsoString(),
        updatedAt: dto.metadata?.updatedAt ?? nowIsoString(),
      },
    };
    validateRule(candidate, taskIds);

    ensure(!this.items.some((rule) => rule.id === candidate.id), `Entity with id ${candidate.id} already exists`);

    const next = [...this.items, candidate].map(cloneRule);
    await this.persist(next);
    return cloneRule(candidate);
  }

  async update(dto: IRecurrenceRuleDto): Promise<IRecurrenceRuleDto> {
    const existing = this.items.find((rule) => rule.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const taskIds = loadTaskIds();
    const candidate: IRecurrenceRuleDto = {
      ...existing!,
      ...dto,
      metadata: {
        ...existing?.metadata,
        ...dto.metadata,
        createdAt: existing?.metadata?.createdAt ?? dto.metadata?.createdAt ?? nowIsoString(),
        updatedAt: nowIsoString(),
      },
    };
    validateRule(candidate, taskIds);

    const next = this.items.map((rule) => (rule.id === candidate.id ? candidate : rule)).map(cloneRule);
    await this.persist(next);
    return cloneRule(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    ensure(!isRuleReferencedByTasks(id), `Cannot delete recurrenceRule ${id}: referenced by tasks`);
    const next = this.items.filter((rule) => rule.id !== id).map(cloneRule);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }
}

export const createRecurrenceRepository = (): IRecurrenceRepository => new RecurrenceRepository();
