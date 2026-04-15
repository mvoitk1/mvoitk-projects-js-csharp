import { apiFetch } from './client'
import type { LoginPayload, RegisterPayload, JWTResponse, LogoutInfo } from '@/types'

export function login(payload: LoginPayload): Promise<JWTResponse> {
  return apiFetch<JWTResponse>('/Account/Login', 'POST', payload)
}

export function register(payload: RegisterPayload): Promise<JWTResponse> {
  return apiFetch<JWTResponse>('/Account/Register', 'POST', payload)
}

export function logout(info: LogoutInfo): Promise<void> {
  return apiFetch<void>('/Account/Logout', 'POST', info)
}
