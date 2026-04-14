import axios from 'axios'
import type { InternalAxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import type { JWTResponse } from '@/types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://taltech.akaver.com'

const apiClient = axios.create({ baseURL: BASE_URL })

let isRefreshing = false
let failedQueue: Array<{
  resolve: (token: string) => void
  reject: (err: unknown) => void
}> = []

function drainQueue(token: string | null, err: unknown) {
  failedQueue.forEach((p) => (token ? p.resolve(token) : p.reject(err)))
  failedQueue = []
}

function decodeExp(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1] ?? ''))
    return typeof payload.exp === 'number' ? payload.exp : null
  } catch {
    return null
  }
}

// Request interceptor — proactive expiry check
apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  let jwt = localStorage.getItem('jwt')
  const refreshToken = localStorage.getItem('refreshToken')

  if (jwt && refreshToken) {
    const exp = decodeExp(jwt)
    if (exp !== null && exp - 30 < Date.now() / 1000) {
      // Token is near expiry — refresh inline using plain axios to avoid loop
      try {
        const res = await axios.post<JWTResponse>(`${BASE_URL}/api/v1.0/Account/RefreshToken`, {
          jwt,
          refreshToken,
        })
        jwt = res.data.token
        localStorage.setItem('jwt', res.data.token)
        localStorage.setItem('refreshToken', res.data.refreshToken)
        localStorage.setItem('firstName', res.data.firstName)
        localStorage.setItem('lastName', res.data.lastName)
      } catch {
        // Let the request proceed; the response interceptor will handle 401
      }
    }
  }

  if (jwt) {
    config.headers = config.headers ?? {}
    config.headers['Authorization'] = `Bearer ${jwt}`
  }

  return config
})

// Response interceptor — handle 401 / 403
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (error.response?.status === 403) {
      // Import router lazily to avoid circular dependency at module init time
      const { default: router } = await import('@/router')
      router.push('/dashboard?error=forbidden')
      return Promise.reject(error)
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({
            resolve: (token: string) => {
              originalRequest.headers['Authorization'] = `Bearer ${token}`
              resolve(apiClient(originalRequest))
            },
            reject,
          })
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      const jwt = localStorage.getItem('jwt')
      const refreshToken = localStorage.getItem('refreshToken')

      try {
        const res = await axios.post<JWTResponse>(`${BASE_URL}/api/v1.0/Account/RefreshToken`, {
          jwt,
          refreshToken,
        })
        const newToken = res.data.token
        localStorage.setItem('jwt', newToken)
        localStorage.setItem('refreshToken', res.data.refreshToken)
        localStorage.setItem('firstName', res.data.firstName)
        localStorage.setItem('lastName', res.data.lastName)

        originalRequest.headers['Authorization'] = `Bearer ${newToken}`
        drainQueue(newToken, null)
        return apiClient(originalRequest)
      } catch (refreshError) {
        drainQueue(null, refreshError)
        localStorage.removeItem('jwt')
        localStorage.removeItem('refreshToken')
        localStorage.removeItem('firstName')
        localStorage.removeItem('lastName')
        window.location.href = '/login'
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(error)
  },
)

export default apiClient
