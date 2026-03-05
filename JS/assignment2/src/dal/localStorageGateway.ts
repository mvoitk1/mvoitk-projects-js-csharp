import type { IStorageEnvelope } from "./types";

// What these next lines do:
// Prefix for all localStorage keys used by this app.
// Why this matters in this project:
// Putting "tm." in one constant avoids repeating raw strings and prevents key-name typos.
const STORAGE_NAMESPACE = "tm";

// What these next lines do:
// List of collections (like tables) we store in localStorage.
// Why this matters in this project:
// This union type lets TypeScript block invalid collection names at compile time.
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

// What these next lines do:
// Thrown when someone tries to save using an old version number.
// Why this matters in this project:
// It protects newer data from being overwritten by stale in-memory state.
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

// What these next lines do:
// Keep all timestamps in one standard format (ISO string).
// Why this matters in this project:
// One shared helper keeps date formatting consistent everywhere this module writes data.
const nowIsoString = (): string => new Date().toISOString();

// What these next lines do:
// Main key used for a collection.
// Why this matters in this project:
// Centralized key creation keeps read/write operations aligned to the exact same key format.
const buildKey = (collection: StorageCollection): string =>
  `${STORAGE_NAMESPACE}.${collection}`;

// What these next lines do:
// Backup key used when existing data is corrupt.
// Why this matters in this project:
// The timestamp suffix prevents old corrupt payloads from being overwritten.
const buildCorruptKey = (collection: StorageCollection): string =>
  `${STORAGE_NAMESPACE}._corrupt.${collection}.${Date.now()}`;

// What these next lines do:
// Writes one whole collection envelope to localStorage.
// Why this matters in this project:
// `<TDto>` means the function works for any item type while preserving type safety.
const persistEnvelope = <TDto>(
  collection: StorageCollection,
  envelope: IStorageEnvelope<TDto>,
): void => {
  localStorage.setItem(buildKey(collection), JSON.stringify(envelope));
};

// What these next lines do:
// Creates an empty collection document when nothing exists yet.
// Why this matters in this project:
// New collections always start from a valid shape (`version`, `updatedAt`, `items`).
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

// What these next lines do:
// If data cannot be parsed/validated, keep a copy for debugging.
// Why this matters in this project:
// You do not lose the bad raw payload, which helps investigate migration/storage bugs later.
const quarantineCorrupt = (
  collection: StorageCollection,
  raw: string,
): void => {
  try {
    localStorage.setItem(buildCorruptKey(collection), raw);
  } catch {
    // What these next lines do:
    // Best-effort quarantine; ignore storage quota failures.
    // Why this matters in this project:
    // Quarantine should never crash normal app flow if storage is full.
  }
};

// What these next lines do:
// Runtime guard to make sure parsed JSON really matches envelope shape.
// Why this matters in this project:
// It prevents invalid JSON shapes from being treated as trusted app data.
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

// What these next lines do:
// Read a collection from localStorage and auto-heal invalid data.
// Why this matters in this project:
// `export` makes this function usable in other files; generic `<TDto>` keeps item types strict.
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

// What these next lines do:
// Optimistic-concurrency save: write only if version still matches.
// Why this matters in this project:
// `export` shares this API, `const` keeps the function binding immutable, and `<TDto>` makes it reusable for tasks/categories/etc.
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
