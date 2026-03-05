import { api } from './apiClient'
import { LoginRequest, LoginResponse, RegisterRequest, RegisterResponse } from '../types/apiTypes'

/**
 * Login a user with email and password
 * Calls POST /auth/login (global endpoint, not tenant-scoped)
 */
export async function loginUser(credentials: LoginRequest): Promise<LoginResponse> {
  return api.post<LoginResponse>('/auth/login', credentials)
}

/**
 * Register a new user account
 * Calls POST /auth/register (global endpoint, not tenant-scoped)
 * Supports Owner mode (creates company) or User mode (joins existing company)
 */
export async function registerUser(payload: RegisterRequest): Promise<RegisterResponse> {
  return api.post<RegisterResponse>('/auth/register', payload)
}
