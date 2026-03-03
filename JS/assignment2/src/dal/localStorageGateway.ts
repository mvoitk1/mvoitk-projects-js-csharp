import type { IStorageEnvelope } from "./types";

const STORAGE_NAMESPACE = "tm";

export type StorageCollection =
  | "tasks"
  | "categories"
  | "priorities"
  | "dependencies"
  | "recurrence"
  | "comments"
  | "attachments"
  | "reminders"
  | "checklist";

export class ConcurrencyError extends Error {
  public readonly collection: StorageCollection;
  public readonly expectedVersion: number;
  public readonly actualVersion: number;

  constructor(
    collection: StorageCollection,
    expectedVersion: number,
    actualVersion: number,
  ) {
    super(
      `Concurrency conflict on ${collection}: expected version ${expectedVersion}, found ${actualVersion}`,
    );
    this.name = "ConcurrencyError";
    this.collection = collection;
    this.expectedVersion = expectedVersion;
    this.actualVersion = actualVersion;
  }
}

const nowIsoString = (): string => new Date().toISOString();

const buildKey = (collection: StorageCollection): string =>
  `${STORAGE_NAMESPACE}.${collection}`;

const buildCorruptKey = (collection: StorageCollection): string =>
  `${STORAGE_NAMESPACE}._corrupt.${collection}.${Date.now()}`;

const persistEnvelope = <TDto>(
  collection: StorageCollection,
  envelope: IStorageEnvelope<TDto>,
): void => {
  localStorage.setItem(buildKey(collection), JSON.stringify(envelope));
};

const initializeEnvelope = <TDto>(
  collection: StorageCollection,
): IStorageEnvelope<TDto> => {
  const envelope: IStorageEnvelope<TDto> = {
    version: 1,
    updatedAt: nowIsoString(),
    items: [],
  };
  persistEnvelope(collection, envelope);
  return envelope;
};

const quarantineCorrupt = (
  collection: StorageCollection,
  raw: string,
): void => {
  try {
    localStorage.setItem(buildCorruptKey(collection), raw);
  } catch {
    // Best-effort quarantine; ignore storage quota failures.
  }
};

const isValidEnvelope = <TDto>(value: unknown): value is IStorageEnvelope<TDto> => {
  if (!value || typeof value !== "object") return false;
  const candidate = value as Partial<IStorageEnvelope<TDto>>;
  return (
    typeof candidate.version === "number" &&
    Number.isFinite(candidate.version) &&
    typeof candidate.updatedAt === "string" &&
    Array.isArray(candidate.items)
  );
};

export const loadEnvelope = <TDto>(
  collection: StorageCollection,
): IStorageEnvelope<TDto> => {
  const key = buildKey(collection);
  const raw = localStorage.getItem(key);

  if (raw === null) {
    return initializeEnvelope<TDto>(collection);
  }

  try {
    const parsed = JSON.parse(raw) as unknown;
    if (isValidEnvelope<TDto>(parsed)) {
      return {
        version: parsed.version,
        updatedAt: parsed.updatedAt,
        items: [...parsed.items],
      };
    }

    quarantineCorrupt(collection, raw);
    return initializeEnvelope<TDto>(collection);
  } catch {
    quarantineCorrupt(collection, raw);
    return initializeEnvelope<TDto>(collection);
  }
};

export const saveEnvelope = <TDto>(
  collection: StorageCollection,
  items: TDto[],
  expectedVersion: number,
): IStorageEnvelope<TDto> => {
  const current = loadEnvelope<TDto>(collection);

  if (current.version !== expectedVersion) {
    throw new ConcurrencyError(collection, expectedVersion, current.version);
  }

  const envelope: IStorageEnvelope<TDto> = {
    version: current.version + 1,
    updatedAt: nowIsoString(),
    items: [...items],
  };

  persistEnvelope(collection, envelope);
  return envelope;
};
