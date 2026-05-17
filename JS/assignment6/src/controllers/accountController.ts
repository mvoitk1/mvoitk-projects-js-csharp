import type { Request, Response } from "express";
import { userModel } from "../models/user.js";
import { hashPassword, issueTokens, rotateRefresh, verifyPassword } from "../auth.js";
import { tokenView } from "../views/account.js";

export const accountController = {
  login(req: Request, res: Response): void {
    const { email, password } = (req.body ?? {}) as { email?: string; password?: string };
    if (!email || !password) {
      res.status(400).json({ title: "email and password required", status: 400 });
      return;
    }
    const user = userModel.findByEmail(email);
    if (!user || !verifyPassword(password, user.passwordHash)) {
      res.status(404).json(["User/Password problem."]);
      return;
    }
    res.json(tokenView(issueTokens(user)));
  },

  register(req: Request, res: Response): void {
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
    if (userModel.findByEmail(email)) {
      res.status(400).json({
        title: "User registration failed.",
        status: 400,
        errors: { email: ["User with this email already exists."] },
      });
      return;
    }
    const user = userModel.create({
      email,
      passwordHash: hashPassword(password),
      firstName: firstName ?? null,
      lastName: lastName ?? null,
    });
    res.json(tokenView(issueTokens(user)));
  },

  refreshToken(req: Request, res: Response): void {
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
    res.json(tokenView(pair));
  },
};
