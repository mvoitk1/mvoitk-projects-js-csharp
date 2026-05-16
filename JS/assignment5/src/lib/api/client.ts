// Fetch wrapper for the TalTech backend.
// - publicFetch:  no auth header (used for Login/Register/RefreshToken).
// - apiFetch:     attaches Bearer <at>; on 401 calls /Account/RefreshToken once,
//                 rewrites cookies, and retries the original request.
// All callers receive a discriminated union — no exceptions for HTTP errors.
// Refresh failure throws Unauthorized so the caller can redirect to /login.

import { clearTokens, readTokens, writeTokens } from "@/lib/auth/tokens";
import type { AccountTokenResponse, ApiErrorBody } from "./contracts";

const BASE_URL =
  process.env.BACKEND_BASE_URL ?? "https://taltech.akaver.com";

export class Unauthorized extends Error {
  constructor(message = "Unauthorized") {
    super(message);
    this.name = "Unauthorized";
  }
}

export interface ApiError {
  status: number;
  body: ApiErrorBody;
  message: string;
}

export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: ApiError };

function buildHeaders(init: RequestInit | undefined, token?: string): Headers {
  const headers = new Headers(init?.headers);
  if (!headers.has("Accept")) headers.set("Accept", "application/json");
  if (init?.body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (token) headers.set("Authorization", `Bearer ${token}`);
  return headers;
}

async function parseBody(res: Response): Promise<unknown> {
  if (res.status === 204) return null;
  const text = await res.text();
  if (!text) return null;
  const ct = res.headers.get("content-type") ?? "";
  if (ct.includes("application/json") || ct.includes("application/problem+json")) {
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }
  return text;
}

async function toResult<T>(res: Response): Promise<ApiResult<T>> {
  const body = await parseBody(res);
  if (res.ok) {
    return { ok: true, data: body as T };
  }
  return {
    ok: false,
    error: {
      status: res.status,
      body: body as ApiErrorBody,
      message:
        (body && typeof body === "object" && "title" in body
          ? String((body as { title?: unknown }).title ?? "")
          : "") || res.statusText || `HTTP ${res.status}`,
    },
  };
}

export async function publicFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<ApiResult<T>> {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: buildHeaders(init),
    cache: "no-store",
  });
  return toResult<T>(res);
}

async function tryRefresh(): Promise<string | null> {
  const { at, rt } = await readTokens();
  if (!at || !rt) return null;
  const res = await fetch(`${BASE_URL}/api/v1/Account/RefreshToken`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({ jwt: at, refreshToken: rt }),
    cache: "no-store",
  });
  if (!res.ok) return null;
  const data = (await res.json()) as AccountTokenResponse;
  if (!data?.token || !data?.refreshToken) return null;
  await writeTokens(data.token, data.refreshToken);
  return data.token;
}

export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<ApiResult<T>> {
  const { at } = await readTokens();
  if (!at) {
    await clearTokens();
    throw new Unauthorized("Missing access token");
  }

  const doRequest = (token: string) =>
    fetch(`${BASE_URL}${path}`, {
      ...init,
      headers: buildHeaders(init, token),
      cache: "no-store",
    });

  let res = await doRequest(at);
  if (res.status !== 401) return toResult<T>(res);

  const refreshed = await tryRefresh();
  if (!refreshed) {
    await clearTokens();
    throw new Unauthorized("Refresh failed");
  }
  res = await doRequest(refreshed);
  if (res.status === 401) {
    await clearTokens();
    throw new Unauthorized("Still 401 after refresh");
  }
  return toResult<T>(res);
}
