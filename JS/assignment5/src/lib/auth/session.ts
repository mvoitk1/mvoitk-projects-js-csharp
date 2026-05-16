// Server-side session check for protected layouts.
//
// Proxy gives an optimistic check (cookie present?). This is the second
// layer: it decodes the JWT to read exp + identity claims, and on expiry
// triggers a refresh by probing an authed endpoint via apiFetch (which
// performs the refresh internally). If the cookie is missing or refresh
// fails, redirect to /login.

import { redirect } from "next/navigation";
import { cache } from "react";
import { apiFetch, Unauthorized } from "@/lib/api/client";
import { readTokens } from "./tokens";

export interface Session {
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  exp: number | null;
}

function decodeJwtPayload(jwt: string): Record<string, unknown> | null {
  const parts = jwt.split(".");
  if (parts.length !== 3) return null;
  try {
    const b64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = b64 + "===".slice(0, (4 - (b64.length % 4)) % 4);
    const json = Buffer.from(padded, "base64").toString("utf8");
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function strClaim(payload: Record<string, unknown> | null, key: string): string | null {
  const v = payload?.[key];
  return typeof v === "string" ? v : null;
}

function buildSession(jwt: string): Session {
  const p = decodeJwtPayload(jwt);
  return {
    email:
      strClaim(p, "email") ??
      strClaim(p, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"),
    firstName:
      strClaim(p, "given_name") ??
      strClaim(p, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"),
    lastName:
      strClaim(p, "family_name") ??
      strClaim(p, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"),
    exp: typeof p?.exp === "number" ? (p.exp as number) : null,
  };
}

// React `cache` deduplicates getSession() across the request render tree.
export const getSession = cache(async (): Promise<Session> => {
  const { at } = await readTokens();
  if (!at) redirect("/login");

  const payload = decodeJwtPayload(at);
  const exp = typeof payload?.exp === "number" ? (payload.exp as number) : null;
  const expired = exp !== null && exp * 1000 <= Date.now();

  if (expired) {
    // Probe an authed endpoint so apiFetch's refresh-on-401 path runs.
    try {
      const res = await apiFetch<unknown>("/api/v1/TodoCategories");
      if (!res.ok && (res.error.status === 401 || res.error.status === 403)) {
        redirect("/login");
      }
    } catch (err) {
      if (err instanceof Unauthorized) redirect("/login");
      throw err;
    }
    const { at: refreshed } = await readTokens();
    if (!refreshed) redirect("/login");
    return buildSession(refreshed);
  }

  return buildSession(at);
});
