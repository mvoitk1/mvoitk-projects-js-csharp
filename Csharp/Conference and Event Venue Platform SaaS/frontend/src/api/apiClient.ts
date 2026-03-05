import { ApiError, ApiErrorResponse } from '../types/apiTypes'

// Re-export ApiError so it can be imported from apiClient
export { ApiError }

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5002'

// Storage keys
const TOKEN_KEY = 'venue_platform_token'
const USER_ID_KEY = 'venue_platform_user_id'
const COMPANY_SLUG_KEY = 'venue_platform_company_slug'

// ==================== Token Management ====================

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}

export function getUserId(): string | null {
  return localStorage.getItem(USER_ID_KEY)
}

export function setUserId(userId: string): void {
  localStorage.setItem(USER_ID_KEY, userId)
}

export function clearUserId(): void {
  localStorage.removeItem(USER_ID_KEY)
}

export function getCompanySlug(): string | null {
  return localStorage.getItem(COMPANY_SLUG_KEY)
}

export function setCompanySlug(slug: string): void {
  localStorage.setItem(COMPANY_SLUG_KEY, slug)
}

export function clearCompanySlug(): void {
  localStorage.removeItem(COMPANY_SLUG_KEY)
}

export function clearAuthData(): void {
  clearToken()
  clearUserId()
  clearCompanySlug()
}

// ==================== Error Handling ====================

function handleApiError(response: Response, errorData: ApiErrorResponse): never {
  const { code, error, details } = errorData
  
  switch (response.status) {
    case 401:
      // Unauthorized - clear auth and trigger redirect
      clearAuthData()
      window.location.href = '/login'
      throw new ApiError(code || 'token_expired', error || 'Session expired', details, 401)
    
    case 403:
      throw new ApiError(code || 'forbidden', error || 'Permission denied', details, 403)
    
    case 409:
      throw new ApiError(code || 'conflict', error || 'Conflict occurred', details, 409)
    
    case 422:
      throw new ApiError(code || 'validation_error', error || 'Validation failed', details, 422)
    
    default:
      throw new ApiError(code || 'internal_error', error || 'An error occurred', details, response.status)
  }
}

/**
 * Normalizes API errors into a structured format with status code preservation.
 * For network errors (backend unreachable), returns status 0 with a user-friendly message.
 */
export function normalizeApiError(error: unknown): { status: number; code: string; message: string; details?: Record<string, string[]> } {
  if (error instanceof ApiError) {
    return {
      status: error.statusCode ?? 0,
      code: error.code,
      message: error.message,
      details: error.details,
    }
  }
  
  if (error instanceof TypeError && error.message.includes('fetch')) {
    // Network error - backend not reachable
    return {
      status: 0,
      code: 'backend_unreachable',
      message: 'Backend not reachable. Is the API running?',
    }
  }
  
  if (error instanceof Error) {
    return {
      status: 0,
      code: 'network_error',
      message: error.message,
    }
  }
  
  return {
    status: 0,
    code: 'unknown_error',
    message: 'An unexpected error occurred',
  }
}

// ==================== API Client ====================

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH'
  body?: unknown
  skipAuth?: boolean
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestOptions = {}
): Promise<T> {
  const { method = 'GET', body, skipAuth = false } = options
  
  const url = `${API_BASE_URL}${endpoint}`
  
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }
  
  // Attach JWT token if available and not skipped
  if (!skipAuth) {
    const token = getToken()
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
  }
  
  const config: RequestInit = {
    method,
    headers,
  }
  
  if (body) {
    config.body = JSON.stringify(body)
  }
  
  try {
    const response = await fetch(url, config)
    
    // Handle empty responses (204 No Content)
    if (response.status === 204) {
      return undefined as T
    }
    
    const data = await response.json()
    
    if (!response.ok) {
      handleApiError(response, data as ApiErrorResponse)
    }
    
    return data as T
  } catch (error) {
    if (error instanceof ApiError) {
      throw error
    }
    
    // Handle network errors (backend not reachable)
    if (error instanceof TypeError) {
      throw new ApiError(
        'backend_unreachable',
        'Backend not reachable. Is the API running?',
        undefined,
        0
      )
    }
    
    // Network or other errors
    throw new ApiError(
      'network_error',
      error instanceof Error ? error.message : 'Network error occurred',
      undefined,
      0
    )
  }
}

// ==================== Convenience Methods ====================

export const api = {
  get: <T>(endpoint: string) => apiRequest<T>(endpoint, { method: 'GET' }),
  
  post: <T>(endpoint: string, body: unknown) =>
    apiRequest<T>(endpoint, { method: 'POST', body }),
  
  put: <T>(endpoint: string, body: unknown) =>
    apiRequest<T>(endpoint, { method: 'PUT', body }),
  
  patch: <T>(endpoint: string, body: unknown) =>
    apiRequest<T>(endpoint, { method: 'PATCH', body }),
  
  delete: <T>(endpoint: string) => apiRequest<T>(endpoint, { method: 'DELETE' }),

  /**
   * POST with form data (URLSearchParams).
   * Use for form-urlencoded endpoints.
   */
  postForm: async <T>(endpoint: string, formData: URLSearchParams): Promise<T> => {
    const url = `${API_BASE_URL}${endpoint}`
    
    const headers: Record<string, string> = {
      // Don't set Content-Type - browser will set it with boundary for form data
    }
    
    const token = getToken()
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
    
    const response = await fetch(url, {
      method: 'POST',
      headers,
      body: formData,
    })
    
    // Handle empty responses (204 No Content)
    if (response.status === 204) {
      return undefined as T
    }
    
    const data = await response.json()
    
    if (!response.ok) {
      handleApiError(response, data as ApiErrorResponse)
    }
    
    return data as T
  },
}

// Backwards-compatible alias
export const apiClient = api