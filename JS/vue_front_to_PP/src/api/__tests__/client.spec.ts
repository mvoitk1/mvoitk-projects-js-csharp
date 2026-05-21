import { describe, it, expect, beforeEach, vi } from 'vitest'
import { apiFetch } from '@/api/client'
import { mockFetchSequence, mockResponse } from '../../../test/helpers/fetch-mock'
import { validJwt, expiredJwt } from '../../../test/helpers/jwt'

const BASE = 'http://test.local/api/v1'

function lastAuthEvent(): Promise<CustomEvent> {
  return new Promise((resolve) => {
    window.addEventListener('auth:logout', (e) => resolve(e as CustomEvent), { once: true })
  })
}

describe('apiFetch — headers', () => {
  it('attaches Authorization Bearer header when a JWT is present', async () => {
    localStorage.setItem('jwt', validJwt())
    const spy = mockFetchSequence([{ json: { ok: true } }])

    await apiFetch('/Products')

    const [, init] = spy.mock.calls[0]
    expect((init?.headers as Record<string, string>)['Authorization']).toBe(
      `Bearer ${localStorage.getItem('jwt')}`,
    )
  })

  it('omits Authorization header when no JWT is present', async () => {
    const spy = mockFetchSequence([{ json: { ok: true } }])

    await apiFetch('/Products')

    const [url, init] = spy.mock.calls[0]
    expect(url).toBe(`${BASE}/Products`)
    expect((init?.headers as Record<string, string>)['Authorization']).toBeUndefined()
  })

  it('sets Content-Type only when a body is provided', async () => {
    const spy = mockFetchSequence([{ json: {} }, { json: {} }])

    await apiFetch('/Cart', 'GET')
    await apiFetch('/Cart', 'POST', { quantity: 1 })

    const getHeaders = spy.mock.calls[0][1]?.headers as Record<string, string>
    const postHeaders = spy.mock.calls[1][1]?.headers as Record<string, string>
    expect(getHeaders['Content-Type']).toBeUndefined()
    expect(postHeaders['Content-Type']).toBe('application/json')
    expect(spy.mock.calls[1][1]?.body).toBe(JSON.stringify({ quantity: 1 }))
  })
})

describe('apiFetch — proactive refresh on expired JWT', () => {
  it('refreshes before the request when the stored JWT is expired', async () => {
    localStorage.setItem('jwt', expiredJwt())
    localStorage.setItem('refreshToken', 'rt-old')
    const fresh = validJwt()
    const spy = mockFetchSequence([
      // RenewRefreshToken
      { json: { jwt: fresh, refreshToken: 'rt-new' } },
      // the actual /Products request
      { json: { ok: true } },
    ])

    await apiFetch('/Products')

    expect(spy.mock.calls[0][0]).toBe(`${BASE}/Account/RenewRefreshToken`)
    expect(spy.mock.calls[1][0]).toBe(`${BASE}/Products`)
    // request carries the refreshed token
    const headers = spy.mock.calls[1][1]?.headers as Record<string, string>
    expect(headers['Authorization']).toBe(`Bearer ${fresh}`)
  })

  it('clears tokens, dispatches auth:logout, and throws when proactive refresh fails', async () => {
    localStorage.setItem('jwt', expiredJwt())
    localStorage.setItem('refreshToken', 'rt-old')
    mockFetchSequence([{ status: 400 }]) // RenewRefreshToken fails

    const event = lastAuthEvent()
    await expect(apiFetch('/Products')).rejects.toThrow('Session expired')
    await event

    expect(localStorage.getItem('jwt')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
  })
})

describe('ensureRefresh — dedupe', () => {
  it('fires exactly one RenewRefreshToken for N concurrent expired-token requests', async () => {
    localStorage.setItem('jwt', expiredJwt())
    localStorage.setItem('refreshToken', 'rt-old')
    const fresh = validJwt()
    const spy = vi.fn(async (url: string) => {
      if (url.endsWith('/Account/RenewRefreshToken')) {
        return mockResponse({ json: { jwt: fresh, refreshToken: 'rt-new' } })
      }
      return mockResponse({ json: { ok: true } })
    })
    vi.stubGlobal('fetch', spy)

    await Promise.all([apiFetch('/A'), apiFetch('/B'), apiFetch('/C')])

    const renewCalls = spy.mock.calls.filter((c) =>
      String(c[0]).endsWith('/Account/RenewRefreshToken'),
    )
    expect(renewCalls).toHaveLength(1)
  })
})

describe('apiFetch — reactive 401 handling', () => {
  it('retries once after a successful refresh on 401', async () => {
    localStorage.setItem('jwt', validJwt())
    localStorage.setItem('refreshToken', 'rt-old')
    const fresh = validJwt({ n: 2 })
    const spy = mockFetchSequence([
      { status: 401 }, // first /Orders -> 401
      { json: { jwt: fresh, refreshToken: 'rt-new' } }, // refresh
      { json: { items: [] } }, // retried /Orders
    ])

    const result = await apiFetch<{ items: unknown[] }>('/Orders')

    expect(result).toEqual({ items: [] })
    expect(spy.mock.calls.map((c) => c[0])).toEqual([
      `${BASE}/Orders`,
      `${BASE}/Account/RenewRefreshToken`,
      `${BASE}/Orders`,
    ])
  })

  it('clears tokens and dispatches auth:logout on 401 + failed refresh', async () => {
    localStorage.setItem('jwt', validJwt())
    localStorage.setItem('refreshToken', 'rt-old')
    mockFetchSequence([
      { status: 401 },
      { status: 400 }, // refresh fails
    ])

    const event = lastAuthEvent()
    await expect(apiFetch('/Orders')).rejects.toThrow('Session expired')
    await event
    expect(localStorage.getItem('jwt')).toBeNull()
  })
})

describe('apiFetch — error body parsing precedence', () => {
  it('prefers title over messages and detail', async () => {
    mockFetchSequence([
      { status: 400, json: { title: 'T', messages: ['M'], detail: 'D' } },
    ])
    await expect(apiFetch('/x')).rejects.toThrow('T')
  })

  it('falls back to messages[0] when title is absent', async () => {
    mockFetchSequence([{ status: 400, json: { messages: ['M'], detail: 'D' } }])
    await expect(apiFetch('/x')).rejects.toThrow('M')
  })

  it('falls back to detail when title and messages are absent', async () => {
    mockFetchSequence([{ status: 400, json: { detail: 'D' } }])
    await expect(apiFetch('/x')).rejects.toThrow('D')
  })

  it('falls back to HTTP {status} for non-JSON error bodies', async () => {
    mockFetchSequence([{ status: 500, text: '<html>boom</html>', contentType: 'text/html' }])
    await expect(apiFetch('/x')).rejects.toThrow('HTTP 500')
  })
})

describe('apiFetch — body parsing', () => {
  it('returns undefined for 204 No Content', async () => {
    mockFetchSequence([{ status: 204 }])
    await expect(apiFetch('/Cart/items/1')).resolves.toBeUndefined()
  })

  it('parses JSON for non-204 success', async () => {
    mockFetchSequence([{ json: { id: '1' } }])
    await expect(apiFetch('/Products/1')).resolves.toEqual({ id: '1' })
  })
})

describe('token events', () => {
  beforeEach(() => {
    localStorage.setItem('jwt', expiredJwt())
    localStorage.setItem('refreshToken', 'rt-old')
  })

  it('dispatches auth:tokens when a refresh saves new tokens', async () => {
    const fresh = validJwt()
    mockFetchSequence([
      { json: { jwt: fresh, refreshToken: 'rt-new' } },
      { json: { ok: true } },
    ])

    const received: Array<{ jwt: string | null }> = []
    window.addEventListener('auth:tokens', (e) => {
      received.push((e as CustomEvent).detail)
    })

    await apiFetch('/Products')
    expect(received.at(-1)).toEqual({ jwt: fresh, refreshToken: 'rt-new' })
  })
})
