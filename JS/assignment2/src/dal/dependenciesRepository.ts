import { loadEnvelope, saveEnvelope } from "./localStorageGateway";
import type {
  EntityId,
  IDependenciesRepository,
  IDependencyDto,
  ITaskDto,
} from "./types";

const ensure = (condition: boolean, message: string): void => {
  if (!condition) {
    throw new Error(message);
  }
};

const cloneDependency = (dep: IDependencyDto): IDependencyDto => ({ ...dep });

const nowIsoString = (): string => new Date().toISOString();

const loadTaskIds = (): Set<EntityId> => {
  const tasks = loadEnvelope<ITaskDto>("tasks").items;
  return new Set(tasks.map((t) => t.id));
};

const buildAdjacency = (edges: IDependencyDto[]): Map<EntityId, EntityId[]> => {
  const adjacency = new Map<EntityId, EntityId[]>();
  edges.forEach((edge) => {
    const list = adjacency.get(edge.taskId) ?? [];
    list.push(edge.dependsOnId);
    adjacency.set(edge.taskId, list);
  });
  return adjacency;
};

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

const validateEdge = (edge: IDependencyDto, taskIds: Set<EntityId>): void => {
  ensure(!!edge.id, "dependency.id is required");
  ensure(!!edge.taskId, "dependency.taskId is required");
  ensure(!!edge.dependsOnId, "dependency.dependsOnId is required");
  ensure(!!edge.type, "dependency.type is required");
  ensure(edge.taskId !== edge.dependsOnId, "dependency cannot self-reference");
  ensure(taskIds.has(edge.taskId), `dependency.taskId ${edge.taskId} does not exist`);
  ensure(taskIds.has(edge.dependsOnId), `dependency.dependsOnId ${edge.dependsOnId} does not exist`);
};

class DependenciesRepository implements IDependenciesRepository {
  private items: IDependencyDto[];
  private version: number;

  constructor() {
    const envelope = loadEnvelope<IDependencyDto>("dependencies");
    this.items = envelope.items.map(cloneDependency);
    this.version = envelope.version;
  }

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
