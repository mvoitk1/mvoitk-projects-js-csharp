// Build fake (unsigned) JWTs for tests. Only the payload segment matters to the
// app, which decodes it with base64url + atob and never verifies the signature.

function base64Url(input: string): string {
  // btoa is available in jsdom.
  return btoa(input).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

export function makeJwt(payload: Record<string, unknown>): string {
  const header = base64Url(JSON.stringify({ alg: 'none', typ: 'JWT' }))
  const body = base64Url(JSON.stringify(payload))
  return `${header}.${body}.sig`
}

/** A token whose `exp` is well in the future. */
export function validJwt(extra: Record<string, unknown> = {}): string {
  return makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600, ...extra })
}

/** A token whose `exp` is already past (also trips the 30 s pre-expiry window). */
export function expiredJwt(extra: Record<string, unknown> = {}): string {
  return makeJwt({ exp: Math.floor(Date.now() / 1000) - 10, ...extra })
}

/** Token carrying an `Admin` role via the plain `role` claim. */
export function adminJwt(): string {
  return validJwt({ role: 'Admin' })
}
