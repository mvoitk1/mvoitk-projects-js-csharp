import { createHash, randomBytes } from "node:crypto";
import { readFileSync, writeFileSync, existsSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import bcrypt from "bcryptjs";
import jwt from "jsonwebtoken";
import type { Request, Response, NextFunction } from "express";
import { refreshTokenModel } from "./models/refreshToken.js";
import { userModel } from "./models/user.js";

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

  refreshTokenModel.create({
    userId: user.id,
    tokenHash: sha256(refreshToken),
    jwtHash: sha256(token),
    expiresAt: new Date(Date.now() + REFRESH_TTL_SEC * 1000).toISOString(),
  });

  return {
    token,
    refreshToken,
    firstName: user.firstName,
    lastName: user.lastName,
  };
}

export function rotateRefresh(jwtString: string, refreshToken: string): TokenPair | null {
  const row = refreshTokenModel.findByTokenHash(sha256(refreshToken));
  if (!row) return null;
  if (row.jwtHash !== sha256(jwtString)) return null;
  if (new Date(row.expiresAt).getTime() < Date.now()) {
    refreshTokenModel.deleteById(row.id);
    return null;
  }
  const user = userModel.findById(row.userId);
  if (!user) return null;

  refreshTokenModel.deleteById(row.id);
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
