import { api } from './apiClient'
import { LoginRequest, LoginResponse } from '../types/apiTypes'

/**
 * Login a user with email and password
 * Calls POST /auth/login (global endpoint, not tenant-scoped)
 */
export async function loginUser(credentials: LoginRequest): Promise<LoginResponse> {
  return api.post<LoginResponse>('/auth/login', credentials)
}
