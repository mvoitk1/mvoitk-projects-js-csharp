import { createHash, randomBytes, randomUUID } from "node:crypto";
import { readFileSync, writeFileSync, existsSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import bcrypt from "bcryptjs";
import jwt from "jsonwebtoken";
import type { Request, Response, NextFunction } from "express";
import { db, now } from "./db.js";

const ACCESS_TTL_SEC = 60 * 30;
const REFRESH_TTL_SEC = 60 * 60 * 24 * 30;

function loadOrCreateSecret(): string {
  if (process.env.JWT_SECRET) return process.env.JWT_SECRET;
  const path = resolve(process.env.JWT_SECRET_PATH ?? "data/.jwt_secret");
  if (existsSync(path)) return readFileSync(path, "utf8").trim();
  mkdirSync(dirname(path), { recursive: true });
  const secret = randomBytes(48).toString("base64url");
  writeFileSync(path, secret, { mode: 0o600 });
  return secret;
}

const JWT_SECRET = loadOrCreateSecret();

export interface JwtPayload {
  sub: string;
  email: string;
}

export function hashPassword(plain: string): string {
  return bcrypt.hashSync(plain, 10);
}

export function verifyPassword(plain: string, hash: string): boolean {
  return bcrypt.compareSync(plain, hash);
}

function sha256(input: string): string {
  return createHash("sha256").update(input).digest("hex");
}

interface UserLite {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
}

export interface TokenPair {
  token: string;
  refreshToken: string;
  firstName: string | null;
  lastName: string | null;
}

export function issueTokens(user: UserLite): TokenPair {
  const token = jwt.sign({ sub: user.id, email: user.email } satisfies JwtPayload, JWT_SECRET, {
    expiresIn: ACCESS_TTL_SEC,
  });
  const refreshToken = randomBytes(32).toString("base64url");

  db.prepare(
    `INSERT INTO refresh_tokens (id, userId, tokenHash, jwtHash, expiresAt, createdDt)
     VALUES (?, ?, ?, ?, ?, ?)`,
  ).run(
    randomUUID(),
    user.id,
    sha256(refreshToken),
    sha256(token),
    new Date(Date.now() + REFRESH_TTL_SEC * 1000).toISOString(),
    now(),
  );

  return {
    token,
    refreshToken,
    firstName: user.firstName,
    lastName: user.lastName,
  };
}

interface RefreshRow {
  id: string;
  userId: string;
  jwtHash: string;
  expiresAt: string;
}

export function rotateRefresh(jwtString: string, refreshToken: string): TokenPair | null {
  const row = db
    .prepare(
      `SELECT id, userId, jwtHash, expiresAt FROM refresh_tokens WHERE tokenHash = ?`,
    )
    .get(sha256(refreshToken)) as RefreshRow | undefined;
  if (!row) return null;
  if (row.jwtHash !== sha256(jwtString)) return null;
  if (new Date(row.expiresAt).getTime() < Date.now()) {
    db.prepare(`DELETE FROM refresh_tokens WHERE id = ?`).run(row.id);
    return null;
  }
  const user = db
    .prepare(`SELECT id, email, firstName, lastName FROM users WHERE id = ?`)
    .get(row.userId) as UserLite | undefined;
  if (!user) return null;

  db.prepare(`DELETE FROM refresh_tokens WHERE id = ?`).run(row.id);
  return issueTokens(user);
}

declare module "express-serve-static-core" {
  interface Request {
    userId?: string;
    userEmail?: string;
  }
}

export function requireAuth(req: Request, res: Response, next: NextFunction): void {
  const header = req.header("authorization") ?? "";
  const [scheme, token] = header.split(" ");
  if (scheme?.toLowerCase() !== "bearer" || !token) {
    res.status(401).end();
    return;
  }
  try {
    const payload = jwt.verify(token, JWT_SECRET) as JwtPayload;
    req.userId = payload.sub;
    req.userEmail = payload.email;
    next();
  } catch {
    res.status(401).end();
  }
}
