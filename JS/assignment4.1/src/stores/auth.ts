import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { authApi } from '@/api/auth'
import type { LoginRequest, RegisterRequest } from '@/types'

function decodeExp(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1] ?? ''))
    return typeof payload.exp === 'number' ? payload.exp : null
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const jwt = ref<string | null>(localStorage.getItem('jwt'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const firstName = ref<string | null>(localStorage.getItem('firstName'))
  const lastName = ref<string | null>(localStorage.getItem('lastName'))

  let _refreshTimer: ReturnType<typeof setTimeout> | null = null

  const isAuthenticated = computed(() => !!jwt.value)
  const currentUser = computed(() =>
    firstName.value && lastName.value ? `${firstName.value} ${lastName.value}` : null,
  )

  function scheduleRefresh(token: string) {
    if (_refreshTimer !== null) {
      clearTimeout(_refreshTimer)
      _refreshTimer = null
    }
    const exp = decodeExp(token)
    if (exp === null) return
    const delay = (exp - 60) * 1000 - Date.now()
    if (delay > 0) {
      _refreshTimer = setTimeout(() => refreshTokens(), delay)
    }
  }

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
    const res = await authApi.login(credentials)
    _persist(res.data.token, res.data.refreshToken, res.data.firstName, res.data.lastName)
  }

  async function register(info: RegisterRequest) {
    const res = await authApi.register(info)
    _persist(res.data.token, res.data.refreshToken, res.data.firstName, res.data.lastName)
  }

  async function refreshTokens(): Promise<boolean> {
    const currentJwt = jwt.value
    const currentRefresh = refreshToken.value
    if (!currentJwt || !currentRefresh) {
      logout()
      return false
    }
    try {
      const res = await authApi.refreshToken({ jwt: currentJwt, refreshToken: currentRefresh })
      _persist(res.data.token, res.data.refreshToken, res.data.firstName, res.data.lastName)
      return true
    } catch {
      logout()
      return false
    }
  }

  function logout() {
    if (_refreshTimer !== null) {
      clearTimeout(_refreshTimer)
      _refreshTimer = null
    }
    jwt.value = null
    refreshToken.value = null
    firstName.value = null
    lastName.value = null
    localStorage.removeItem('jwt')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('firstName')
    localStorage.removeItem('lastName')

    // Reset data stores manually (no $reset() in Composition API style stores)
    import('@/stores/todoTasks').then(({ useTodoTasksStore }) => {
      const s = useTodoTasksStore()
      s.items = []
      s.loading = false
      s.error = null
    })
    import('@/stores/todoCategory').then(({ useTodoCategoryStore }) => {
      const s = useTodoCategoryStore()
      s.items = []
      s.loading = false
      s.error = null
    })
    import('@/stores/todoPriority').then(({ useTodoPriorityStore }) => {
      const s = useTodoPriorityStore()
      s.items = []
      s.loading = false
      s.error = null
    })
  }

  // Schedule refresh on store init if a token is already present
  if (jwt.value) scheduleRefresh(jwt.value)

  return {
    jwt,
    refreshToken,
    firstName,
    lastName,
    isAuthenticated,
    currentUser,
    login,
    register,
    refreshTokens,
    logout,
  }
})
