import { loadEnvelope, saveEnvelope } from "./localStorageGateway";
import type {
  EntityId,
  IDependenciesRepository,
  IDependencyDto,
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

const cloneDependency = (dep: IDependencyDto): IDependencyDto => ({ ...dep });

// What these next lines do:
// Standard timestamp format used by this layer.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const nowIsoString = (): string => new Date().toISOString();

// What these next lines do:
// Load valid task IDs so dependencies cannot point to missing tasks.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const loadTaskIds = (): Set<EntityId> => {
  const tasks = loadEnvelope<ITaskDto>("tasks").items;
  return new Set(tasks.map((t) => t.id));
};

// What these next lines do:
// Build graph adjacency list: task -> tasks it depends on.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const buildAdjacency = (edges: IDependencyDto[]): Map<EntityId, EntityId[]> => {
  const adjacency = new Map<EntityId, EntityId[]>();
  edges.forEach((edge) => {
    const list = adjacency.get(edge.taskId) ?? [];
    list.push(edge.dependsOnId);
    adjacency.set(edge.taskId, list);
  });
  return adjacency;
};

// What these next lines do:
// DFS cycle check so dependencies cannot become circular.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const hasCycle = (edges: IDependencyDto[]): boolean => {
  const adjacency = buildAdjacency(edges);
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

// What these next lines do:
// Validate one dependency edge.
// Why this matters in this project:
// Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
const validateEdge = (edge: IDependencyDto, taskIds: Set<EntityId>): void => {
  ensure(!!edge.id, "dependency.id is required");
  ensure(!!edge.taskId, "dependency.taskId is required");
  ensure(!!edge.dependsOnId, "dependency.dependsOnId is required");
  ensure(!!edge.type, "dependency.type is required");
  ensure(edge.taskId !== edge.dependsOnId, "dependency cannot self-reference");
  ensure(taskIds.has(edge.taskId), `dependency.taskId ${edge.taskId} does not exist`);
  ensure(taskIds.has(edge.dependsOnId), `dependency.dependsOnId ${edge.dependsOnId} does not exist`);
};

// What these next lines do:
// CRUD repository for dependency edges.
// Why this matters in this project:
// Encapsulating this logic in a class keeps related state and behavior together, which makes the flow easier to maintain.
class DependenciesRepository implements IDependenciesRepository {
  private items: IDependencyDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<IDependencyDto>("dependencies");
    this.items = envelope.items.map(cloneDependency);
    this.version = envelope.version;
  }

  // What these next lines do:
  // Persist collection with optimistic version check.
  // Why this matters in this project:
  // The version check avoids overwriting newer writes from another operation or tab.
  private async persist(next: IDependencyDto[]): Promise<void> {
    const envelope = saveEnvelope<IDependencyDto>("dependencies", next, this.version);
    this.items = envelope.items.map(cloneDependency);
    this.version = envelope.version;
  }

  async findAll(): Promise<IDependencyDto[]> {
    return this.items.map(cloneDependency);
  }

  async findById(id: EntityId): Promise<IDependencyDto | undefined> {
    const found = this.items.find((edge) => edge.id === id);
    return found ? cloneDependency(found) : undefined;
  }

  async create(dto: IDependencyDto): Promise<IDependencyDto> {
    const taskIds = loadTaskIds();
    const candidate: IDependencyDto = {
      ...dto,
      createdAt: dto.createdAt ?? nowIsoString(),
    };
    validateEdge(candidate, taskIds);

    ensure(!this.items.some((edge) => edge.id === candidate.id), `Entity with id ${candidate.id} already exists`);

    const next = [...this.items, candidate];
    ensure(!hasCycle(next), "dependency cycle detected");

    await this.persist(next.map(cloneDependency));
    return cloneDependency(candidate);
  }

  async update(dto: IDependencyDto): Promise<IDependencyDto> {
    const existing = this.items.find((edge) => edge.id === dto.id);
    ensure(!!existing, `Entity with id ${dto.id} not found`);

    const taskIds = loadTaskIds();
    const candidate: IDependencyDto = {
      ...existing!,
      ...dto,
      createdAt: existing?.createdAt ?? dto.createdAt ?? nowIsoString(),
    };
    validateEdge(candidate, taskIds);

    const next = this.items.map((edge) => (edge.id === candidate.id ? candidate : edge));
    ensure(!hasCycle(next), "dependency cycle detected");

    await this.persist(next.map(cloneDependency));
    return cloneDependency(candidate);
  }

  async delete(id: EntityId): Promise<void> {
    const next = this.items.filter((edge) => edge.id !== id).map(cloneDependency);
    ensure(next.length !== this.items.length, `Entity with id ${id} not found`);
    await this.persist(next);
  }
}

export const createDependenciesRepository = (): IDependenciesRepository =>
  new DependenciesRepository();
