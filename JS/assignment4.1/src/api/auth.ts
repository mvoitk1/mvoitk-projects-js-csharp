import apiClient from './axios'
import type { JWTResponse, LoginRequest, RegisterRequest, RefreshTokenRequest } from '@/types'

export const authApi = {
  login: (data: LoginRequest) => apiClient.post<JWTResponse>('/api/v1.0/Account/Login', data),
  register: (data: RegisterRequest) =>
    apiClient.post<JWTResponse>('/api/v1.0/Account/Register', data),
  refreshToken: (data: RefreshTokenRequest) =>
    apiClient.post<JWTResponse>('/api/v1.0/Account/RefreshToken', data),
}
