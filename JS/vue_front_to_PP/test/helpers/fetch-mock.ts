import { vi } from 'vitest'

export interface MockResponseInit {
  status?: number
  /** JSON body — serialized and returned by res.json(). */
  json?: unknown
  /** Raw body text (used when `json` is not given). */
  text?: string
  /** content-type header; defaults to application/json when `json` is set. */
  contentType?: string
}

/** Build a minimal Response-like object covering what apiFetch reads. */
export function mockResponse(init: MockResponseInit = {}): Response {
  const status = init.status ?? 200
  const contentType =
    init.contentType ?? (init.json !== undefined ? 'application/json' : 'text/plain')
  const headers = new Headers({ 'content-type': contentType })
  return {
    ok: status >= 200 && status < 300,
    status,
    headers,
    json: async () => init.json,
    text: async () => init.text ?? '',
  } as unknown as Response
}

/**
 * Install a fetch mock that returns the queued responses in order. Each entry
 * is either a MockResponseInit or a function (url, init) => MockResponseInit.
 * Returns the underlying vi.fn() spy for assertions.
 */
export function mockFetchSequence(
  responses: Array<MockResponseInit | ((url: string, init?: RequestInit) => MockResponseInit)>,
) {
  let i = 0
  const spy = vi.fn(async (url: string, init?: RequestInit) => {
    const entry = responses[Math.min(i, responses.length - 1)]
    i++
    const resolved = typeof entry === 'function' ? entry(url, init) : entry
    return mockResponse(resolved)
  })
  vi.stubGlobal('fetch', spy)
  return spy
}
