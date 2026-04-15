import type { RefreshTokenModel, JWTResponse } from '@/types'

const BASE_URL = import.meta.env.VITE_API_BASE as string

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

let isRefreshing = false

function getJwt(): string | null {
  return localStorage.getItem('jwt')
}

function getRefreshToken(): string | null {
  return localStorage.getItem('refreshToken')
}

function saveTokens(jwt: string, refreshToken: string): void {
  localStorage.setItem('jwt', jwt)
  localStorage.setItem('refreshToken', refreshToken)
}

function clearTokens(): void {
  localStorage.removeItem('jwt')
  localStorage.removeItem('refreshToken')
}

async function renewToken(): Promise<boolean> {
  const jwt = getJwt()
  const refreshToken = getRefreshToken()
  if (!jwt || !refreshToken) return false

  const body: RefreshTokenModel = { jwt, refreshToken }
  const res = await fetch(`${BASE_URL}/Account/RenewRefreshToken`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) return false

  const data: JWTResponse = await res.json()
  if (data.jwt && data.refreshToken) {
    saveTokens(data.jwt, data.refreshToken)
    return true
  }
  return false
}

export async function apiFetch<T>(
  path: string,
  method: HttpMethod = 'GET',
  body?: unknown,
  retry = true,
): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  const jwt = getJwt()
  if (jwt) headers['Authorization'] = `Bearer ${jwt}`

  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (res.status === 401 && retry && !isRefreshing) {
    isRefreshing = true
    const ok = await renewToken()
    isRefreshing = false
    if (ok) return apiFetch<T>(path, method, body, false)
    clearTokens()
    window.dispatchEvent(new CustomEvent('auth:logout'))
    throw new Error('Session expired')
  }

  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const err = await res.json()
      if (err?.title) message = err.title
      else if (err?.messages?.[0]) message = err.messages[0]
    } catch {
      // ignore parse errors
    }
    throw new Error(message)
  }

  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}
