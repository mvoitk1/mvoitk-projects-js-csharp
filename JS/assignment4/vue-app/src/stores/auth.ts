import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '../api/auth'
import type { LoginRequest, RegisterRequest } from '../types'

export const useAuthStore = defineStore('auth', () => {
  const jwt = ref<string | null>(localStorage.getItem('jwt'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const firstName = ref<string | null>(localStorage.getItem('firstName'))
  const lastName = ref<string | null>(localStorage.getItem('lastName'))

  const isAuthenticated = computed(() => !!jwt.value)
  const currentUser = computed(() => firstName.value ? `${firstName.value} ${lastName.value}` : null)

  function _persist(token: string, refresh: string, first: string, last: string) {
    jwt.value = token
    refreshToken.value = refresh
    firstName.value = first
    lastName.value = last
    localStorage.setItem('jwt', token)
    localStorage.setItem('refreshToken', refresh)
    localStorage.setItem('firstName', first)
    localStorage.setItem('lastName', last)
  }

  async function login(credentials: LoginRequest) {
    const { data } = await authApi.login(credentials)
    _persist(data.token, data.refreshToken, data.firstName, data.lastName)
  }

  async function register(info: RegisterRequest) {
    const { data } = await authApi.register(info)
    _persist(data.token, data.refreshToken, data.firstName, data.lastName)
  }

  async function refreshTokens() {
    if (!jwt.value || !refreshToken.value) return false
    try {
      const { data } = await authApi.refreshToken({ jwt: jwt.value, refreshToken: refreshToken.value })
      _persist(data.token, data.refreshToken, data.firstName, data.lastName)
      return true
    } catch {
      logout()
      return false
    }
  }

  function logout() {
    jwt.value = null
    refreshToken.value = null
    firstName.value = null
    lastName.value = null
    localStorage.removeItem('jwt')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('firstName')
    localStorage.removeItem('lastName')
  }

  return { jwt, refreshToken, firstName, lastName, isAuthenticated, currentUser, login, register, logout, refreshTokens }
})
