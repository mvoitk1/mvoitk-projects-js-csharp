import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import { makeJwt, validJwt } from '../../../test/helpers/jwt'

vi.mock('@/api/auth', () => ({
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  renewToken: vi.fn(),
}))

import * as authApi from '@/api/auth'

const mocked = vi.mocked(authApi)

describe('auth store — login / register', () => {
  it('persists tokens to state and localStorage on login', async () => {
    mocked.login.mockResolvedValue({ jwt: 'j1', refreshToken: 'r1' })
    const store = useAuthStore()

    await store.login({ email: 'a@b.c', password: 'pw' })

    expect(store.jwt).toBe('j1')
    expect(store.refreshToken).toBe('r1')
    expect(store.isLoggedIn).toBe(true)
    expect(localStorage.getItem('jwt')).toBe('j1')
  })

  it('does not persist when the API returns null tokens', async () => {
    mocked.login.mockResolvedValue({ jwt: null, refreshToken: null })
    const store = useAuthStore()

    await store.login({ email: 'a@b.c', password: 'pw' })

    expect(store.jwt).toBeNull()
    expect(localStorage.getItem('jwt')).toBeNull()
  })

  it('persists tokens on register', async () => {
    mocked.register.mockResolvedValue({ jwt: 'j2', refreshToken: 'r2' })
    const store = useAuthStore()

    await store.register({ firstName: 'A', lastName: 'B', email: 'a@b.c', password: 'pw' })

    expect(store.jwt).toBe('j2')
  })
})

describe('auth store — isAdmin role decoding', () => {
  it('detects Admin via the plain `role` claim', () => {
    localStorage.setItem('jwt', makeJwt({ role: 'Admin' }))
    expect(useAuthStore().isAdmin).toBe(true)
  })

  it('detects Admin via the Microsoft schema claim URI', () => {
    localStorage.setItem(
      'jwt',
      makeJwt({ 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin' }),
    )
    expect(useAuthStore().isAdmin).toBe(true)
  })

  it('detects Admin when the role claim is an array', () => {
    localStorage.setItem('jwt', makeJwt({ role: ['User', 'Admin'] }))
    expect(useAuthStore().isAdmin).toBe(true)
  })

  it('is false for a non-admin token and for no token', () => {
    localStorage.setItem('jwt', makeJwt({ role: 'User' }))
    expect(useAuthStore().isAdmin).toBe(false)
    localStorage.removeItem('jwt')
    expect(useAuthStore().isAdmin).toBe(false)
  })
})

describe('auth store — events', () => {
  it('updates state on the auth:tokens event', () => {
    const store = useAuthStore()
    window.dispatchEvent(new CustomEvent('auth:tokens', { detail: { jwt: 'jx', refreshToken: 'rx' } }))
    expect(store.jwt).toBe('jx')
    expect(store.refreshToken).toBe('rx')
  })

  it('clears state on the auth:logout event', () => {
    localStorage.setItem('jwt', validJwt())
    const store = useAuthStore()
    window.dispatchEvent(new CustomEvent('auth:logout'))
    expect(store.jwt).toBeNull()
    expect(localStorage.getItem('jwt')).toBeNull()
  })
})

describe('auth store — logout', () => {
  it('calls the API then clears tokens', async () => {
    mocked.logout.mockResolvedValue(undefined as never)
    localStorage.setItem('jwt', 'j')
    localStorage.setItem('refreshToken', 'r')
    const store = useAuthStore()

    await store.logout()

    expect(mocked.logout).toHaveBeenCalledWith({ refreshToken: 'r' })
    expect(store.jwt).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
  })

  it('swallows API errors and still clears tokens', async () => {
    mocked.logout.mockRejectedValue(new Error('network'))
    localStorage.setItem('refreshToken', 'r')
    const store = useAuthStore()

    await expect(store.logout()).resolves.toBeUndefined()
    expect(store.refreshToken).toBeNull()
  })
})

describe('auth store — periodic renew interval', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('renews tokens every 4 minutes while logged in', async () => {
    mocked.renewToken.mockResolvedValue({ jwt: 'jr', refreshToken: 'rr' })
    localStorage.setItem('jwt', 'j')
    localStorage.setItem('refreshToken', 'r')
    const store = useAuthStore()

    await vi.advanceTimersByTimeAsync(4 * 60 * 1000)

    expect(mocked.renewToken).toHaveBeenCalledWith({ jwt: 'j', refreshToken: 'r' })
    expect(store.jwt).toBe('jr')
  })

  it('skips renewal when not logged in', async () => {
    const store = useAuthStore()
    expect(store.isLoggedIn).toBe(false)

    await vi.advanceTimersByTimeAsync(4 * 60 * 1000)

    expect(mocked.renewToken).not.toHaveBeenCalled()
  })
})
