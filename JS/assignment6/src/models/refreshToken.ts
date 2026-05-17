import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";

export interface RefreshTokenRow {
  id: string;
  userId: string;
  tokenHash: string;
  jwtHash: string;
  expiresAt: string;
  createdDt: string;
}

export const refreshTokenModel = {
  create(input: {
    userId: string;
    tokenHash: string;
    jwtHash: string;
    expiresAt: string;
  }): void {
    db.prepare(
      `INSERT INTO refresh_tokens (id, userId, tokenHash, jwtHash, expiresAt, createdDt)
       VALUES (?, ?, ?, ?, ?, ?)`,
    ).run(randomUUID(), input.userId, input.tokenHash, input.jwtHash, input.expiresAt, now());
  },

  findByTokenHash(tokenHash: string): RefreshTokenRow | undefined {
    return db
      .prepare(
        `SELECT id, userId, tokenHash, jwtHash, expiresAt, createdDt
         FROM refresh_tokens WHERE tokenHash = ?`,
      )
      .get(tokenHash) as RefreshTokenRow | undefined;
  },

  deleteById(id: string): void {
    db.prepare(`DELETE FROM refresh_tokens WHERE id = ?`).run(id);
  },
};
