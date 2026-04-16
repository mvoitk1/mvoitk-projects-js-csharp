import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as authApi from '@/api/auth'
import type { LoginPayload, RegisterPayload } from '@/types'

function decodePayload(jwt: string): Record<string, unknown> {
  try {
    const base64Url = jwt.split('.')[1] ?? ''
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
    return JSON.parse(atob(padded))
  } catch {
    return {}
  }
}

function hasAdminRole(jwt: string | null): boolean {
  if (!jwt) return false

  const payload = decodePayload(jwt)
  const roleClaims = [
    payload.role,
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
  ]

  return roleClaims.some((claim) => {
    if (Array.isArray(claim)) return claim.includes('Admin')
    return claim === 'Admin'
  })
}

export const useAuthStore = defineStore('auth', () => {
  const jwt = ref<string | null>(localStorage.getItem('jwt'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isLoggedIn = computed(() => !!jwt.value)
  const isAdmin = computed(() => hasAdminRole(jwt.value))

  function _persist(j: string, rt: string) {
    jwt.value = j
    refreshToken.value = rt
    localStorage.setItem('jwt', j)
    localStorage.setItem('refreshToken', rt)
  }

  function _clear() {
    jwt.value = null
    refreshToken.value = null
    localStorage.removeItem('jwt')
    localStorage.removeItem('refreshToken')
  }

  async function login(payload: LoginPayload) {
    const res = await authApi.login(payload)
    if (res.jwt && res.refreshToken) _persist(res.jwt, res.refreshToken)
  }

  async function register(payload: RegisterPayload) {
    const res = await authApi.register(payload)
    if (res.jwt && res.refreshToken) _persist(res.jwt, res.refreshToken)
  }

  async function logout() {
    const rt = refreshToken.value
    if (rt) {
      try {
        await authApi.logout({ refreshToken: rt })
      } catch {
        // ignore errors on logout
      }
    }
    _clear()
  }

  window.addEventListener('auth:tokens', ((event: Event) => {
    const { detail } = event as CustomEvent<{ jwt: string | null; refreshToken: string | null }>
    jwt.value = detail.jwt
    refreshToken.value = detail.refreshToken
  }) as EventListener)

  // Listen for the logout event dispatched by the API client on 401 + failed refresh
  window.addEventListener('auth:logout', () => _clear())

  // Proactively renew tokens every 4 minutes while logged in, so idle users
  // are not logged out (mirrors MVC cookie sliding-expiration behaviour).
  setInterval(async () => {
    if (!jwt.value || !refreshToken.value) return
    try {
      const res = await authApi.renewToken({ jwt: jwt.value, refreshToken: refreshToken.value })
      if (res.jwt && res.refreshToken) _persist(res.jwt, res.refreshToken)
    } catch {
      // silent — the next real API call will handle expiry if it comes to that
    }
  }, 4 * 60 * 1000)

  return { jwt, refreshToken, isLoggedIn, isAdmin, login, register, logout }
})
