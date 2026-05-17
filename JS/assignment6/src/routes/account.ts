import { Router, type Request, type Response } from "express";
import { randomUUID } from "node:crypto";
import { db, now } from "../db.js";
import { hashPassword, issueTokens, rotateRefresh, verifyPassword } from "../auth.js";

const router = Router();

interface UserRow {
  id: string;
  email: string;
  passwordHash: string;
  firstName: string | null;
  lastName: string | null;
}

router.post("/Login", (req: Request, res: Response) => {
  const { email, password } = (req.body ?? {}) as { email?: string; password?: string };
  if (!email || !password) {
    res.status(400).json({ title: "email and password required", status: 400 });
    return;
  }
  const user = db
    .prepare(`SELECT id, email, passwordHash, firstName, lastName FROM users WHERE email = ?`)
    .get(email) as UserRow | undefined;
  if (!user || !verifyPassword(password, user.passwordHash)) {
    res.status(404).json(["User/Password problem."]);
    return;
  }
  res.json(issueTokens(user));
});

router.post("/Register", (req: Request, res: Response) => {
  const { email, password, firstName, lastName } = (req.body ?? {}) as {
    email?: string;
    password?: string;
    firstName?: string;
    lastName?: string;
  };
  if (!email || !password) {
    res.status(400).json({ title: "email and password required", status: 400 });
    return;
  }
  const exists = db.prepare(`SELECT 1 FROM users WHERE email = ?`).get(email);
  if (exists) {
    res.status(400).json({
      title: "User registration failed.",
      status: 400,
      errors: { email: ["User with this email already exists."] },
    });
    return;
  }
  const id = randomUUID();
  db.prepare(
    `INSERT INTO users (id, email, passwordHash, firstName, lastName, createdDt)
     VALUES (?, ?, ?, ?, ?, ?)`,
  ).run(id, email, hashPassword(password), firstName ?? null, lastName ?? null, now());

  res.json(
    issueTokens({
      id,
      email,
      firstName: firstName ?? null,
      lastName: lastName ?? null,
    }),
  );
});

router.post("/RefreshToken", (req: Request, res: Response) => {
  const { jwt: jwtString, refreshToken } = (req.body ?? {}) as {
    jwt?: string;
    refreshToken?: string;
  };
  if (!jwtString || !refreshToken) {
    res.status(400).json({ title: "jwt and refreshToken required", status: 400 });
    return;
  }
  const pair = rotateRefresh(jwtString, refreshToken);
  if (!pair) {
    res.status(404).json(["RefreshToken problem."]);
    return;
  }
  res.json(pair);
});

export default router;
