import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";

export interface UserRow {
  id: string;
  email: string;
  passwordHash: string;
  firstName: string | null;
  lastName: string | null;
  createdDt: string;
}

const COLS = "id, email, passwordHash, firstName, lastName, createdDt";

export const userModel = {
  findByEmail(email: string): UserRow | undefined {
    return db.prepare(`SELECT ${COLS} FROM users WHERE email = ?`).get(email) as
      | UserRow
      | undefined;
  },

  findById(id: string): UserRow | undefined {
    return db.prepare(`SELECT ${COLS} FROM users WHERE id = ?`).get(id) as
      | UserRow
      | undefined;
  },

  create(input: {
    email: string;
    passwordHash: string;
    firstName: string | null;
    lastName: string | null;
  }): UserRow {
    const id = randomUUID();
    const createdDt = now();
    db.prepare(
      `INSERT INTO users (id, email, passwordHash, firstName, lastName, createdDt)
       VALUES (?, ?, ?, ?, ?, ?)`,
    ).run(id, input.email, input.passwordHash, input.firstName, input.lastName, createdDt);
    return { id, createdDt, ...input };
  },
};
