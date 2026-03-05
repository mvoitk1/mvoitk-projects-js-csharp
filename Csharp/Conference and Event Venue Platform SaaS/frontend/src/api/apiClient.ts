/**
 * =====================================================================================
 * API CLIENT - The Central Hub for All Backend Communication
 * =====================================================================================
 * 
 * This file is like the "telephone operator" of our application. When our frontend
 * (what the user sees) wants to talk to the backend (the server), it goes through here.
 * 
 * THINK OF IT LIKE THIS:
 * - Frontend = Customer at a restaurant
 * - API Client = Waiter taking the order to the kitchen
 * - Backend/Kitchen = The server that prepares the data
 * 
 * =====================================================================================
 * KEY CONCEPTS EXPLAINED FOR STUDENTS:
 * =====================================================================================
 * 
 * 1. WHAT IS AN API CLIENT?
 *    An API client is a set of functions that make it easy to communicate with a web API.
 *    Instead of writing complex code every time we want data, we use these helper functions.
 * 
 * 2. WHAT IS "fetch"?
 *    fetch() is a built-in JavaScript function that sends network requests.
 *    It's like making a phone call - you dial (make request) and wait for an answer (response).
 *    The syntax: fetch(url, options)
 * 
 * 3. WHAT IS localStorage?
 *    localStorage is a browser feature that stores data on the user's computer.
 *    It persists even after closing the browser! We use it to store:
 *    - JWT token (who the user is)
 *    - User ID
 *    - Company slug (which company they're viewing)
 * 
 *    Think of it like saving your preferences in a video game - they're there when you come back.
 * 
 * 4. WHAT IS A JWT TOKEN?
 *    JWT (JSON Web Token) is like an ID card or badge. When you log in, the server gives
 *    you a token. You show this token with every request to prove "I am who I say I am"
 *    
 *    The token looks like: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." (random-looking string)
 * 
 * 5. WHAT DOES "await" DO?
 *    The "await" keyword PAUSES the code until a Promise resolves.
 *    
 *    WITHOUT await:
 *      const data = fetchData()  // Returns immediately with a Promise object
 *      console.log(data)         // Prints "Promise { <pending> }" - not the actual data!
 *    
 *    WITH await:
 *      const data = await fetchData()  // Waits for the Promise to finish
 *      console.log(data)               // Prints the actual data!
 * 
 * 6. WHAT ARE HTTP METHODS?
 *    - GET    = Ask for data (like reading a webpage)
 *    - POST   = Send data to create something new
 *    - PUT    = Send data to update something
 *    - DELETE = Remove something
 *    - PATCH  = Partially update something
 * 
 * 7. WHAT IS "<T>" (GENERICS)?
 *    Generics let you write flexible, reusable code while still maintaining type safety.
 *    <T> means "some type that will be specified later".
 *    
 *    Example: api.get<User>("/user") means "get me a User object"
 *             api.get<Array<User>>("/users") means "get me an array of Users"
 * 
 * 8. WHAT IS "interface"?
 *    An interface is like a contract or blueprint. It defines what properties an object
 *    should have. TypeScript uses this to catch errors before the code runs!
 * 
 * 9. WHAT IS "never"?
 *    The return type "never" means this function will NEVER return a value.
 *    It's used for functions that always throw an error or run forever (infinite loop).
 *    Our handleApiError() always throws, so it returns never.
 * 
 * =====================================================================================
 */

import { ApiError, ApiErrorResponse } from '../types/apiTypes'

// Re-export ApiError so it can be imported from apiClient
// This is a convenience - instead of importing from both places,
// you can just import from apiClient and get everything you need
export { ApiError }

// ==================== Configuration ====================

// Use || instead of ?? to catch empty strings too
// import.meta.env.VITE_API_BASE_URL is an environment variable
// This lets us easily change the API URL between development and production
// Default: 'http://localhost:5002' (our local development server)
const rawBaseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5002') as string

/**
 * normalizeBaseUrl - Cleans up the URL to prevent errors
 * 
 * Key concepts:
 * - .trim() removes whitespace from start/end (e.g., " http://test " -> "http://test")
 * - .replace(/\/$/, '') removes trailing slashes
 *   Why? Because if baseUrl = "http://api.com/" and endpoint = "/spaces",
 *   we'd get "http://api.com//spaces" (double slash - bad!)
 */
function normalizeBaseUrl(url: string): string {
  const trimmed = url.trim()
  if (!trimmed) {
    throw new Error('API_BASE_URL is empty')
  }
  // Remove trailing slash to avoid double slashes when concatenating with paths
  return trimmed.replace(/\/$/, '')
}

const API_BASE_URL = normalizeBaseUrl(rawBaseUrl)

// ==================== Safe URL Builder ====================

/**
 * buildApiUrl - Safely combines base URL and endpoint path
 * 
 * This prevents common URL mistakes that cause errors, especially in Safari.
 * 
 * @param baseUrl - The base API URL (e.g., "http://localhost:5002")
 * @param endpoint - The API path (e.g., "/my-company/spaces")
 * @returns The complete URL
 * 
 * WHAT IS "try/catch"?
 * Try/catch is how we handle errors in JavaScript. Code that might fail goes in "try",
 * and if it fails, the "catch" block handles the error instead of crashing the app.
 * 
 * Example:
 *   try {
 *     riskyOperation()  // This might fail
 *   } catch (error) {
 *     console.log("Something went wrong!")  // Handle the error gracefully
 *   }
 */
function buildApiUrl(baseUrl: string, endpoint: string): string {
  let finalUrl = ''
  try {
    const trimmedEndpoint = endpoint.trim()
    
    // Ensure endpoint starts with / - this is required for proper URL joining
    // If user passes "spaces", we make it "/spaces"
    const normalizedPath = trimmedEndpoint.startsWith('/') 
      ? trimmedEndpoint 
      : `/${trimmedEndpoint}`
    
    // Validate base URL starts with http:// or https://
    // URLs must have a protocol to be valid!
    if (!baseUrl.startsWith('http://') && !baseUrl.startsWith('https://')) {
      throw new Error(`Invalid base URL scheme: ${baseUrl}`)
    }
    
    // Combine the URLs
    finalUrl = `${baseUrl}${normalizedPath}`
    
    // Try to construct URL to catch any invalid patterns
    // The URL constructor will throw if the URL is invalid
    void new URL(finalUrl)
    
    return finalUrl
  } catch (error) {
    // Format a nice error message
    const errorMessage = error instanceof Error ? error.message : 'Unknown URL error'
    
    // Only log errors in development mode (import.meta.env.DEV)
    // This keeps production logs clean
    if (import.meta.env.DEV) {
      console.error('[apiClient] URL construction failed:', {
        baseUrl,
        endpointPath: endpoint,
        finalUrl,
        error: errorMessage,
      })
    }
    
    // Throw an ApiError with all the details
    throw new ApiError(
      'config_error',
      `Invalid API URL configuration: ${errorMessage}`,
      { baseUrl: [baseUrl], endpoint: [endpoint] },
      0
    )
  }
}

// ==================== Token Management ====================

// Storage keys - These are the "names" we use to save data in localStorage
// Using constants prevents typos (we'll get an error if we misspell a constant)
const TOKEN_KEY = 'venue_platform_token'
const USER_ID_KEY = 'venue_platform_user_id'
const COMPANY_SLUG_KEY = 'venue_platform_company_slug'

/**
 * getToken - Retrieves the JWT token from browser storage
 * 
 * @returns The token string, or null if not found
 * 
 * localStorage.getItem() returns null if the key doesn't exist
 * 
 * WHAT IS "null"?
 * null is a special value that means "intentionally empty" or "no value".
 * It's different from undefined (which means "not yet assigned").
 */
export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

/**
 * setToken - Saves the JWT token to browser storage
 * 
 * @param token - The JWT token string from login response
 * 
 * localStorage.setItem(key, value) stores data as strings
 * We store the raw token string
 */
export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

/**
 * clearToken - Removes the token from storage (logs user out)
 */
export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}

// Similar functions for user ID and company slug
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

/**
 * clearAuthData - Clears all authentication-related data at once
 * 
 * Call this when logging out to remove all user data from the browser
 */
export function clearAuthData(): void {
  clearToken()
  clearUserId()
  clearCompanySlug()
}

// ==================== Error Handling ====================

/**
 * handleApiError - Processes error responses from the API
 * 
 * This function takes the HTTP response status and error data,
 * then throws the appropriate ApiError with helpful information.
 * 
 * @param response - The fetch Response object (contains status code)
 * @param errorData - The parsed JSON error response from server
 * @returns never - This function always throws an error
 * 
 * HTTP STATUS CODES EXPLAINED:
 * - 200 = Success! Everything worked
 * - 201 = Created successfully (like POST completed)
 * - 400 = Bad request - your request was malformed
 * - 401 = Unauthorized - you're not logged in
 * - 403 = Forbidden - you don't have permission
 * - 404 = Not found - the resource doesn't exist
 * - 409 = Conflict - something conflicts (like double booking)
 * - 422 = Validation error - check your input
 * - 500 = Server error - the backend broke
 */
function handleApiError(response: Response, errorData: ApiErrorResponse): never {
  // Destructure the error response - pull out specific properties
  const { code, error, details } = errorData
  
  // switch statement - Check the status code and handle each case differently
  // This is like if/else but cleaner when checking many conditions
  switch (response.status) {
    case 401:
      // Unauthorized - Token probably expired
      // Clear auth data and redirect to login
      clearAuthData()
      window.location.href = '/login'
      throw new ApiError(code || 'token_expired', error || 'Session expired', details, 401)
    
    case 403:
      // Forbidden - User doesn't have permission
      throw new ApiError(code || 'forbidden', error || 'Permission denied', details, 403)
    
    case 409:
      // Conflict - Resource conflict (e.g., double booking)
      throw new ApiError(code || 'conflict', error || 'Conflict occurred', details, 409)
    
    case 422:
      // Validation error - The data sent was invalid
      throw new ApiError(code || 'validation_error', error || 'Validation failed', details, 422)
    
    default:
      // Any other error code
      throw new ApiError(code || 'internal_error', error || 'An error occurred', details, response.status)
  }
}

/**
 * normalizeApiError - Converts various error types into a consistent format
 * 
 * This helps the UI display errors uniformly regardless of what caused them.
 * 
 * @param error - Any error that might have occurred
 * @returns A standardized error object with status, code, message, and details
 * 
 * WHAT IS "unknown"?
 * The type "unknown" is like "any" but safer. With "any", you can do anything.
 * With "unknown", you must check the type first before using it.
 * This prevents mistakes!
 */
export function normalizeApiError(error: unknown): { status: number; code: string; message: string; details?: Record<string, string[]> } {
  // instanceof checks if an object is an instance of a class
  // This is safer than checking properties that might not exist
  if (error instanceof ApiError) {
    return {
      status: error.statusCode ?? 0,  // ?? means "use left side if not null/undefined, else use right side"
      code: error.code,
      message: error.message,
      details: error.details,
    }
  }
  
  // TypeError is thrown when network fails (e.g., backend unreachable)
  if (error instanceof TypeError && error.message.includes('fetch')) {
    return {
      status: 0,
      code: 'backend_unreachable',
      message: 'Backend not reachable. Is the API running?',
    }
  }
  
  // Generic JavaScript Error
  if (error instanceof Error) {
    return {
      status: 0,
      code: 'network_error',
      message: error.message,
    }
  }
  
  // Completely unknown error - should rarely happen
  return {
    status: 0,
    code: 'unknown_error',
    message: 'An unexpected error occurred',
  }
}

// ==================== API Client ====================

/**
 * RequestOptions - Configuration for API requests
 * 
 * This interface defines all the optional parameters you can pass
 * when making an API request.
 * 
 * WHAT IS "interface"?
 * An interface is a TypeScript way of defining the "shape" of an object.
 * It tells TypeScript what properties should exist and what types they should be.
 * 
 * Example:
 *   let options: RequestOptions = {
 *     method: 'POST',
 *     body: { name: 'John' },
 *     skipAuth: false
 *   }
 */
export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH'  // The HTTP method (default: GET)
  body?: unknown                                      // Data to send (optional)
  skipAuth?: boolean                                  // Skip adding token (for public endpoints)
}

/**
 * apiRequest - The core function that makes HTTP requests
 * 
 * This is the main workhorse of the API client. All other methods
 * (get, post, put, etc.) eventually call this function.
 * 
 * @param endpoint - The API path (e.g., "/my-company/spaces")
 * @param options - Request configuration (method, body, etc.)
 * @returns Promise<T> - The response data, typed as T
 * 
 * @template T - A generic type parameter that specifies what kind of data to expect
 * 
 * WHAT IS A GENERIC FUNCTION?
 * <T> is a "type parameter" - it's a placeholder that gets filled in when you call the function.
 * 
 * Example:
 *   const user = await apiRequest<User>('/user/1')     // T = User
 *   const users = await apiRequest<User[]>('/users')  // T = User[]
 * 
 * This lets us have ONE function that works with ANY data type!
 */
export async function apiRequest<T>(
  endpoint: string,
  options: RequestOptions = {}
): Promise<T> {
  // Destructuring - Pull out properties from options object
  // This is shorthand for: const method = options.method || 'GET'
  const { method = 'GET', body, skipAuth = false } = options
  
  // Build the full URL using our safe URL builder
  const url = buildApiUrl(API_BASE_URL, endpoint)
  
  // Create the headers object
  // Record<string, string> is TypeScript shorthand for "an object with string keys and string values"
  const headers: Record<string, string> = {
    // Tell the server we're sending JSON data
    'Content-Type': 'application/json',
  }
  
  // If this endpoint needs authentication and we're not skipping it...
  if (!skipAuth) {
    // Get the token from localStorage
    const token = getToken()
    // If we have a token, add it to the Authorization header
    // The format is: "Bearer <token>"
    // "Bearer" is just a convention - it means "the holder of this token"
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
  }
  
  // Build the fetch configuration
  // RequestInit is the standard type for fetch options
  const config: RequestInit = {
    method,  // GET, POST, PUT, etc.
    headers, // Our headers object with Content-Type and possibly Auth
  }
  
  // If there's a body, convert it to JSON string
  // JSON.stringify() turns a JavaScript object into a JSON string
  // This is necessary because fetch can only send strings!
  if (body) {
    config.body = JSON.stringify(body)
  }
  
  try {
    // Actually make the network request!
    // This is where the magic happens - we send the request to the server
    const response = await fetch(url, config)
    
    // Handle empty responses (204 No Content)
    // 204 means the request succeeded but there's no data to return
    // (common for DELETE requests)
    if (response.status === 204) {
      return undefined as T
    }
    
    // Parse the JSON response
    // response.json() returns a Promise that resolves to the parsed JSON
    const data = await response.json()
    
    // If the response wasn't OK (status not 200-299), handle the error
    if (!response.ok) {
      handleApiError(response, data as ApiErrorResponse)
    }
    
    // Return the data, cast to type T
    return data as T
  } catch (error) {
    // If it's already an ApiError, just re-throw it
    if (error instanceof ApiError) {
      throw error
    }
    
    // Handle network errors (when the backend is unreachable)
    // TypeError with "fetch" in the message typically means network failure
    if (error instanceof TypeError) {
      throw new ApiError(
        'backend_unreachable',
        'Backend not reachable. Is the API running?',
        undefined,
        0
      )
    }
    
    // Generic network or other errors
    throw new ApiError(
      'network_error',
      error instanceof Error ? error.message : 'Network error occurred',
      undefined,
      0
    )
  }
}

// ==================== Convenience Methods ====================

/**
 * api - A convenient wrapper around apiRequest with preset HTTP methods
 * 
 * Instead of writing:
 *   apiRequest('/endpoint', { method: 'GET' })
 * 
 * You can write:
 *   api.get('/endpoint')
 * 
 * This is much cleaner and easier to read!
 * 
 * WHAT IS "const api = {...}"?
 * This creates an OBJECT with methods. It's like a mini-module.
 * Each property is a function that calls apiRequest with specific options.
 */
export const api = {
  /**
   * GET request - Fetch data from the server
   * 
   * @template T - The type of data expected in response
   * @param endpoint - The API path
   * @returns Promise<T> - The response data
   * 
   * Example:
   *   const spaces = await api.get<Space[]>('/company/spaces')
   */
  get: <T>(endpoint: string) => apiRequest<T>(endpoint, { method: 'GET' }),
  
  /**
   * POST request - Send data to create something new
   * 
   * @template T - The type of data expected in response
   * @param endpoint - The API path
   * @param body - The data to send
   * @returns Promise<T> - The response data
   */
  post: <T>(endpoint: string, body: unknown) =>
    apiRequest<T>(endpoint, { method: 'POST', body }),
  
  /**
   * PUT request - Update/replace an existing resource
   * 
   * PUT vs PATCH:
   * - PUT = Replace the entire resource with new data
   * - PATCH = Only update specific fields (partial update)
   */
  put: <T>(endpoint: string, body: unknown) =>
    apiRequest<T>(endpoint, { method: 'PUT', body }),
  
  /**
   * PATCH request - Partially update a resource
   */
  patch: <T>(endpoint: string, body: unknown) =>
    apiRequest<T>(endpoint, { method: 'PATCH', body }),
  
  /**
   * DELETE request - Remove a resource
   */
  delete: <T>(endpoint: string) => apiRequest<T>(endpoint, { method: 'DELETE' }),

  /**
   * POST with form data (URLSearchParams)
   * 
   * Used for endpoints that expect form-urlencoded data instead of JSON.
   * This is less common but needed for some OAuth endpoints.
   * 
   * @param endpoint - The API path
   * @param formData - URLSearchParams object with the data
   * @returns Promise<T> - The response data
   */
  postForm: async <T>(endpoint: string, formData: URLSearchParams): Promise<T> => {
    const url = buildApiUrl(API_BASE_URL, endpoint)
    
    const headers: Record<string, string> = {
      // Don't set Content-Type - browser will set it with boundary for form data
      // If we set it manually, the boundary would be missing!
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
    
    // Handle empty responses
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
// This lets old code that imports "apiClient" still work
export const apiClient = api
