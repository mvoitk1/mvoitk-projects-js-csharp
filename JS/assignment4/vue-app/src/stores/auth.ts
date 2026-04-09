import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '../api/auth'
import type { LoginRequest, RegisterRequest } from '../types'
import { useTodoTasksStore } from './todoTasks'
import { useTodoCategoryStore } from './todoCategory'
import { useTodoPriorityStore } from './todoPriority'

let _refreshTimer: ReturnType<typeof setTimeout> | null = null

export const useAuthStore = defineStore('auth', () => {
  const jwt = ref<string | null>(localStorage.getItem('jwt'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const firstName = ref<string | null>(localStorage.getItem('firstName'))
  const lastName = ref<string | null>(localStorage.getItem('lastName'))

  const isAuthenticated = computed(() => !!jwt.value)
  const currentUser = computed(() => firstName.value ? `${firstName.value} ${lastName.value}` : null)

  function scheduleRefresh(token: string) {
    if (_refreshTimer) clearTimeout(_refreshTimer)
    try {
      const payload = JSON.parse(atob(token.split('.')[1]))
      const msUntilRefresh = (payload.exp - 60) * 1000 - Date.now()
      if (msUntilRefresh > 0) {
        _refreshTimer = setTimeout(() => refreshTokens(), msUntilRefresh)
      }
    } catch {
      // malformed token — do nothing
    }
  }

  // NOTE: Tokens are stored in localStorage because the backend returns them
  // in the JSON response body and does not support Set-Cookie / httpOnly.
  // The CSP headers in nginx.conf and the absence of v-html are the primary
  // XSS mitigations in place of httpOnly cookie storage.
  function _persist(token: string, refresh: string, first: string, last: string) {
    jwt.value = token
    refreshToken.value = refresh
    firstName.value = first
    lastName.value = last
    localStorage.setItem('jwt', token)
    localStorage.setItem('refreshToken', refresh)
    localStorage.setItem('firstName', first)
    localStorage.setItem('lastName', last)
    scheduleRefresh(token)
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
    if (_refreshTimer) { clearTimeout(_refreshTimer); _refreshTimer = null }
    jwt.value = null
    refreshToken.value = null
    firstName.value = null
    lastName.value = null
    localStorage.removeItem('jwt')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('firstName')
    localStorage.removeItem('lastName')
    const tasksStore = useTodoTasksStore()
    const categoryStore = useTodoCategoryStore()
    const priorityStore = useTodoPriorityStore()
    tasksStore.items = []; tasksStore.loading = false; tasksStore.error = null
    categoryStore.items = []; categoryStore.loading = false; categoryStore.error = null
    priorityStore.items = []; priorityStore.loading = false; priorityStore.error = null
  }

  return { jwt, refreshToken, firstName, lastName, isAuthenticated, currentUser, login, register, logout, refreshTokens }
})
