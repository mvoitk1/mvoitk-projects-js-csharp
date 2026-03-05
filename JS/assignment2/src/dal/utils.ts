import type { EntityId, ICrudRepository } from "./types";

type Equality<T> = (a: T, b: T) => boolean;

// What these next lines do:
// Lightweight clone used to avoid returning direct internal references.
// Why this matters in this project:
// The generic type parameter lets one helper work with many data shapes while still preserving compile-time type checks.
const shallowClone = <T>(value: T): T => {
  if (Array.isArray(value)) {
    return [...value] as unknown as T;
  }
  if (value && typeof value === "object") {
    return { ...(value as Record<string, unknown>) } as T;
  }
  return value;
};

// What these next lines do:
// Config for building a generic in-memory CRUD repository.
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Config for building a generic in-memory CRUD repository.
export interface CrudRepositoryOptions<TDto, TId extends EntityId = EntityId> {
  getId: (dto: TDto) => TId;
  initialItems?: TDto[];
  equals?: Equality<TId>;
  persist?: (items: TDto[]) => Promise<void> | void;
}

// What these next lines do:
// Build a comparator that sorts by multiple fields in order.
// Why this matters in this project:
// Using `export` makes this function available to other files, `const` keeps the function binding stable, and the generic type parameter keeps item types safe across different collections.
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

// What these next lines do:
// Describes adds/updates/deletes relative to a base list.
// Why this matters in this project:
// Standardizing date handling avoids subtle bugs when comparing or displaying time values.
export interface Changeset<TDto, TId> {
  created?: TDto[];
  updated?: TDto[];
  deleted?: TId[];
}

// What these next lines do:
// Tiny wrappers to standardize mapper call signatures.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
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

// What these next lines do:
// Build a fast lookup map by one property key.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
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

// What these next lines do:
// Apply a changeset to a base list (update, create, delete).
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
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

// What these next lines do:
// Factory for a reusable in-memory CRUD repository implementation.
// Why this matters in this project:
// Exporting this constant/function exposes a single shared implementation so other modules do not duplicate logic.
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
