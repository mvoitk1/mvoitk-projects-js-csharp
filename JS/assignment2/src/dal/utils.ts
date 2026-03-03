import type { EntityId, ICrudRepository } from "./types";

type Equality<T> = (a: T, b: T) => boolean;

const shallowClone = <T>(value: T): T => {
  if (Array.isArray(value)) {
    return [...value] as unknown as T;
  }
  if (value && typeof value === "object") {
    return { ...(value as Record<string, unknown>) } as T;
  }
  return value;
};

export interface CrudRepositoryOptions<TDto, TId extends EntityId = EntityId> {
  getId: (dto: TDto) => TId;
  initialItems?: TDto[];
  equals?: Equality<TId>;
  persist?: (items: TDto[]) => Promise<void> | void;
}

export const compareByFields = <T>(
  fields: { selector: (item: T) => unknown; direction?: "asc" | "desc" }[],
): ((a: T, b: T) => number) => {
  return (a: T, b: T): number => {
    for (const { selector, direction = "asc" } of fields) {
      const av = selector(a);
      const bv = selector(b);

      if (av === bv) continue;
      if (av === undefined || av === null) return direction === "asc" ? 1 : -1;
      if (bv === undefined || bv === null) return direction === "asc" ? -1 : 1;

      if (av < bv) return direction === "asc" ? -1 : 1;
      if (av > bv) return direction === "asc" ? 1 : -1;
    }
    return 0;
  };
};

export interface Changeset<TDto, TId> {
  created?: TDto[];
  updated?: TDto[];
  deleted?: TId[];
}

export const mapDtoToDomain = <TDto, TDomain, TContext = unknown>(
  dto: TDto,
  mapper: (dto: TDto, context?: TContext) => TDomain,
  context?: TContext,
): TDomain => mapper(dto, context);

export const mapDomainToDto = <TDomain, TDto, TContext = unknown>(
  domain: TDomain,
  mapper: (domain: TDomain, context?: TContext) => TDto,
  context?: TContext,
): TDto => mapper(domain, context);

export const buildIndex = <TItem, TKey extends keyof TItem>(
  items: TItem[],
  key: TKey,
): Map<TItem[TKey], TItem> => {
  const index = new Map<TItem[TKey], TItem>();
  items.forEach((item) => {
    index.set(item[key], item);
  });
  return index;
};

export const applyChangeset = <TDto, TId>(
  base: TDto[],
  getId: (dto: TDto) => TId,
  changeset: Changeset<TDto, TId>,
): TDto[] => {
  const next = new Map<TId, TDto>();
  base.forEach((item) => {
    next.set(getId(item), shallowClone(item));
  });

  (changeset.updated ?? []).forEach((item) => {
    next.set(getId(item), shallowClone(item));
  });

  (changeset.created ?? []).forEach((item) => {
    next.set(getId(item), shallowClone(item));
  });

  (changeset.deleted ?? []).forEach((id) => {
    next.delete(id);
  });

  return Array.from(next.values());
};

export const createCrudRepository = <TDto, TId extends EntityId = EntityId>({
  getId,
  initialItems = [],
  equals = (a: TId, b: TId) => a === b,
  persist,
}: CrudRepositoryOptions<TDto, TId>): ICrudRepository<TDto> => {
  let items = [...initialItems];

  const persistIfNeeded = async (): Promise<void> => {
    if (persist) {
      await persist(items);
    }
  };

  const findAll = async (): Promise<TDto[]> => items.map((item) => shallowClone(item));

  const findById = async (id: TId): Promise<TDto | undefined> => {
    const found = items.find((item) => equals(getId(item), id));
    return found ? shallowClone(found) : undefined;
  };

  const create = async (dto: TDto): Promise<TDto> => {
    const id = getId(dto);
    if (items.some((item) => equals(getId(item), id))) {
      throw new Error(`Entity with id ${String(id)} already exists`);
    }
    items = [...items, shallowClone(dto)];
    await persistIfNeeded();
    return shallowClone(dto);
  };

  const update = async (dto: TDto): Promise<TDto> => {
    const id = getId(dto);
    let updated = false;

    items = items.map((existing) => {
      if (equals(getId(existing), id)) {
        updated = true;
        return shallowClone(dto);
      }
      return existing;
    });

    if (!updated) {
      throw new Error(`Entity with id ${String(id)} not found`);
    }

    await persistIfNeeded();
    return shallowClone(dto);
  };

  const remove = async (id: TId): Promise<void> => {
    const nextItems = items.filter((item) => !equals(getId(item), id));
    if (nextItems.length === items.length) {
      throw new Error(`Entity with id ${String(id)} not found`);
    }
    items = nextItems;
    await persistIfNeeded();
  };

  return {
    findAll,
    findById,
    create,
    update,
    delete: remove,
  };
};
