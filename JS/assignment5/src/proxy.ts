// Next 16 Proxy (renamed from Middleware). Optimistic auth gate only —
// real validation happens server-side via getSession() in protected layouts
// and per-request inside server actions.

import { NextResponse, type NextRequest } from "next/server";
import { AT_COOKIE } from "@/lib/auth/tokens";

const PROTECTED_PREFIXES = ["/todos", "/categories", "/priorities"];
const AUTH_PATHS = ["/login", "/register"];

function isProtected(pathname: string): boolean {
  return PROTECTED_PREFIXES.some(
    (p) => pathname === p || pathname.startsWith(`${p}/`),
  );
}

export function proxy(req: NextRequest): NextResponse {
  const { pathname } = req.nextUrl;
  const hasAccess = Boolean(req.cookies.get(AT_COOKIE)?.value);

  if (isProtected(pathname) && !hasAccess) {
    const url = req.nextUrl.clone();
    url.pathname = "/login";
    url.search = "";
    return NextResponse.redirect(url);
  }

  if (hasAccess && AUTH_PATHS.includes(pathname)) {
    const url = req.nextUrl.clone();
    url.pathname = "/todos";
    url.search = "";
    return NextResponse.redirect(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|.*\\.(?:png|jpg|jpeg|svg|gif|ico|webp)$).*)"],
};
