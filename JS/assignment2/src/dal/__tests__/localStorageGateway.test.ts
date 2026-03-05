import { describe, expect, it, beforeEach, vi } from "vitest";
import {
  ConcurrencyError,
  loadEnvelope,
  saveEnvelope,
  type StorageCollection,
} from "../localStorageGateway";
import type { IStorageEnvelope } from "../types";

// What these next lines do:
// Clear browser localStorage so each test starts from a blank state.
// Why this matters in this project:
// Storage tests are stateful, so leftover keys from previous tests would cause false failures.
const clear = (): void => localStorage.clear();

// What these next lines do:
// Insert a full envelope object into the exact storage key used by the gateway.
// Why this matters in this project:
// Tests can simulate version conflicts/corrupt data precisely, without calling higher layers.
const seed = <T>(collection: StorageCollection, envelope: IStorageEnvelope<T>): void => {
  localStorage.setItem(`tm.${collection}`, JSON.stringify(envelope));
};

describe("localStorageGateway", () => {
  beforeEach(() => {
    clear();
    vi.useRealTimers();
  });

  it("initializes missing collection with version 1 and empty items", () => {
    const envelope = loadEnvelope<unknown>("tasks");

    expect(envelope.version).toBe(1);
    expect(envelope.items).toEqual([]);
    expect(typeof envelope.updatedAt).toBe("string");

    const persisted = JSON.parse(localStorage.getItem("tm.tasks") ?? "null");
    expect(persisted).toMatchObject({ version: 1, items: [] });
  });

  it("persists saveEnvelope and increments version", () => {
    seed("tasks", { version: 1, updatedAt: new Date(0).toISOString(), items: [] });

    const result = saveEnvelope("tasks", [{ id: "1" }], 1);

    expect(result.version).toBe(2);
    expect(result.items).toEqual([{ id: "1" }]);

    const persisted = JSON.parse(localStorage.getItem("tm.tasks") ?? "null");
    expect(persisted.version).toBe(2);
    expect(persisted.items).toEqual([{ id: "1" }]);
  });

  it("throws ConcurrencyError when expectedVersion mismatches", () => {
    seed("tasks", { version: 3, updatedAt: new Date(0).toISOString(), items: [] });

    expect(() => saveEnvelope("tasks", [], 2)).toThrow(ConcurrencyError);
  });

  it("quarantines malformed JSON and reinitializes envelope", () => {
    localStorage.setItem("tm.tasks", "not-json");

    const envelope = loadEnvelope<unknown>("tasks");

    expect(envelope.version).toBe(1);
    expect(envelope.items).toEqual([]);

    const quarantineKey = Object.keys(localStorage).find((key) => key.startsWith("tm._corrupt.tasks."));
    expect(quarantineKey).toBeDefined();
    expect(localStorage.getItem(quarantineKey!)).toBe("not-json");
  });

  it("quarantines invalid envelope shape and reinitializes", () => {
    seed("tasks", { version: "oops" } as unknown as IStorageEnvelope<unknown>);

    const envelope = loadEnvelope<unknown>("tasks");

    expect(envelope.version).toBe(1);
    expect(envelope.items).toEqual([]);

    const quarantineKey = Object.keys(localStorage).find((key) => key.startsWith("tm._corrupt.tasks."));
    expect(quarantineKey).toBeDefined();
  });
});
