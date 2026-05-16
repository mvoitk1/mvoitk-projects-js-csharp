// httpOnly cookie helpers for the JWT access/refresh pair.
// Access tokens are short-lived; refresh tokens are long-lived.
// We don't have backend-published TTLs, so we pick sane defaults.

import { cookies } from "next/headers";

export const AT_COOKIE = "at";
export const RT_COOKIE = "rt";

// 1 hour for the access token, 30 days for the refresh token.
// These are upper bounds — the backend enforces actual validity.
const AT_MAX_AGE_SECONDS = 60 * 60;
const RT_MAX_AGE_SECONDS = 60 * 60 * 24 * 30;

function isProd() {
  return process.env.NODE_ENV === "production";
}

export async function readTokens(): Promise<{
  at: string | undefined;
  rt: string | undefined;
}> {
  const jar = await cookies();
  return {
    at: jar.get(AT_COOKIE)?.value,
    rt: jar.get(RT_COOKIE)?.value,
  };
}

export async function writeTokens(at: string, rt: string): Promise<void> {
  const jar = await cookies();
  const base = {
    httpOnly: true,
    sameSite: "lax" as const,
    secure: isProd(),
    path: "/",
  };
  jar.set(AT_COOKIE, at, { ...base, maxAge: AT_MAX_AGE_SECONDS });
  jar.set(RT_COOKIE, rt, { ...base, maxAge: RT_MAX_AGE_SECONDS });
}

export async function clearTokens(): Promise<void> {
  const jar = await cookies();
  jar.delete(AT_COOKIE);
  jar.delete(RT_COOKIE);
}
