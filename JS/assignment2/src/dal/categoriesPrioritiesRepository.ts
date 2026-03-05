import { loadEnvelope, saveEnvelope } from "./localStorageGateway";
import type {
  EntityId,
  ICategoryDto,
  ICategoriesRepository,
  IPriorityDto,
  IPrioritiesRepository,
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

const cloneCategory = (category: ICategoryDto): ICategoryDto => ({ ...category });
const clonePriority = (priority: IPriorityDto): IPriorityDto => ({ ...priority });

// What these next lines do:
// Standard timestamp format used by this layer.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const nowIsoString = (): string => new Date().toISOString();

// What these next lines do:
// Basic required-field checks.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const validateCategory = (category: ICategoryDto): void => {
  ensure(!!category.id, "category.id is required");
  ensure(!!category.name, "category.name is required");
};

const validatePriority = (priority: IPriorityDto): void => {
  ensure(!!priority.id, "priority.id is required");
  ensure(!!priority.label, "priority.label is required");
  ensure(!!priority.value, "priority.value is required");
};

// What these next lines do:
// Prevent deleting values that tasks still use.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const tasksReferencing = (field: "categoryId" | "priorityId", id: EntityId): boolean => {
  const tasks = loadEnvelope<ITaskDto>("tasks").items;
  return tasks.some((task) => task[field] === id);
};

// What these next lines do:
// CRUD repository for categories.
// Why this matters in this project:
// Encapsulating this logic in a class keeps related state and behavior together, which makes the flow easier to maintain.
class CategoriesRepository implements ICategoriesRepository {
  private items: ICategoryDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<ICategoryDto>("categories");
    this.items = envelope.items.map(cloneCategory);
    this.version = envelope.version;
  }

  // What these next lines do:
  // Persist category list with optimistic version check.
  // Why this matters in this project:
  // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Persist category list with optimistic version check.
  private async persist(next: ICategoryDto[]): Promise<void> {
    const envelope = saveEnvelope<ICategoryDto>("categories", next, this.version);
    this.items = envelope.items.map(cloneCategory);
    this.version = envelope.version;
  }

  async findAll(): Promise<ICategoryDto[]> {
    return this.items.map(cloneCategory);
  }

  async findById(id: EntityId): Promise<ICategoryDto | undefined> {
    const found = this.items.find((category) => category.id === id);
    return found ? cloneCategory(found) : undefined;
  }

  async create(dto: ICategoryDto): Promise<ICategoryDto> {
    const candidate: ICategoryDto = {
      ...dto,
      metadata: dto.metadata ?? { createdAt: nowIsoString(), updatedAt: nowIsoString() },
    };
    validateCategory(candidate);
    ensure(!this.items.some((c) => c.id === candidate.id), `Entity with id ${candidate.id} already exists`);

    const next = [...this.items, candidate].map(cloneCategory);
    await this.persist(next);
    return cloneCategory(candidate);
  }

  async update(dto: ICategoryDto): Promise<ICategoryDto> {
    const existing = this.items.find((c) => c.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const candidate: ICategoryDto = {
      ...existing!,
      ...dto,
      metadata: {
        ...existing?.metadata,
        ...dto.metadata,
        updatedAt: nowIsoString(),
        createdAt: existing?.metadata?.createdAt ?? dto.metadata?.createdAt ?? nowIsoString(),
      },
    };
    validateCategory(candidate);

    const next = this.items.map((c) => (c.id === candidate.id ? candidate : c)).map(cloneCategory);
    await this.persist(next);
    return cloneCategory(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    ensure(!tasksReferencing("categoryId", id), `Cannot delete category ${id}: referenced by tasks`);
    const next = this.items.filter((c) => c.id !== id).map(cloneCategory);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }
}

// What these next lines do:
// CRUD repository for priorities.
// Why this matters in this project:
// Encapsulating this logic in a class keeps related state and behavior together, which makes the flow easier to maintain.
class PrioritiesRepository implements IPrioritiesRepository {
  private items: IPriorityDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<IPriorityDto>("priorities");
    this.items = envelope.items.map(clonePriority);
    this.version = envelope.version;
  }

  // What these next lines do:
  // Persist priority list with optimistic version check.
  // Why this matters in this project:
  // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Persist priority list with optimistic version check.
  private async persist(next: IPriorityDto[]): Promise<void> {
    const envelope = saveEnvelope<IPriorityDto>("priorities", next, this.version);
    this.items = envelope.items.map(clonePriority);
    this.version = envelope.version;
  }

  async findAll(): Promise<IPriorityDto[]> {
    return this.items.map(clonePriority);
  }

  async findById(id: EntityId): Promise<IPriorityDto | undefined> {
    const found = this.items.find((priority) => priority.id === id);
    return found ? clonePriority(found) : undefined;
  }

  async create(dto: IPriorityDto): Promise<IPriorityDto> {
    const candidate: IPriorityDto = {
      ...dto,
      metadata: dto.metadata ?? { createdAt: nowIsoString(), updatedAt: nowIsoString() },
    };
    validatePriority(candidate);
    ensure(!this.items.some((p) => p.id === candidate.id), `Entity with id ${candidate.id} already exists`);

    const next = [...this.items, candidate].map(clonePriority);
    await this.persist(next);
    return clonePriority(candidate);
  }

  async update(dto: IPriorityDto): Promise<IPriorityDto> {
    const existing = this.items.find((p) => p.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const candidate: IPriorityDto = {
      ...existing!,
      ...dto,
      metadata: {
        ...existing?.metadata,
        ...dto.metadata,
        updatedAt: nowIsoString(),
        createdAt: existing?.metadata?.createdAt ?? dto.metadata?.createdAt ?? nowIsoString(),
      },
    };
    validatePriority(candidate);

    const next = this.items.map((p) => (p.id === candidate.id ? candidate : p)).map(clonePriority);
    await this.persist(next);
    return clonePriority(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    ensure(!tasksReferencing("priorityId", id), `Cannot delete priority ${id}: referenced by tasks`);
    const next = this.items.filter((p) => p.id !== id).map(clonePriority);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }
}

export const createCategoriesRepository = (): ICategoriesRepository => new CategoriesRepository();

export const createPrioritiesRepository = (): IPrioritiesRepository => new PrioritiesRepository();
