import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as authApi from '@/api/auth'
import type { LoginPayload, RegisterPayload } from '@/types'

function decodePayload(jwt: string): Record<string, unknown> {
  try {
    return JSON.parse(atob(jwt.split('.')[1] ?? ''))
  } catch {
    return {}
  }
}

export const useAuthStore = defineStore('auth', () => {
  const jwt = ref<string | null>(localStorage.getItem('jwt'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isLoggedIn = computed(() => !!jwt.value)
  const isAdmin = computed(() => {
    if (!jwt.value) return false
    const payload = decodePayload(jwt.value)
    return payload['role'] === 'Admin'
  })

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

  // Listen for the logout event dispatched by the API client on 401 + failed refresh
  window.addEventListener('auth:logout', () => _clear())

  return { jwt, refreshToken, isLoggedIn, isAdmin, login, register, logout }
})
