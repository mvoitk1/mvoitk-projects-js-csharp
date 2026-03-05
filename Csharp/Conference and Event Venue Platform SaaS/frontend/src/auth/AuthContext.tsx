import React, { createContext, useState, useCallback, useEffect } from 'react'
import { LoginRequest, LoginResponse } from '../types/apiTypes'
import { api } from '../api/apiClient'
import {
  getToken,
  setToken,
  getUserId,
  setUserId,
  clearAuthData,
  getCompanySlug,
  setCompanySlug,
} from '../api/apiClient'

export interface AuthState {
  token: string | null
  userId: string | null
  companySlug: string | null
  isAuthenticated: boolean
  isLoading: boolean
}

export interface AuthContextType extends AuthState {
  login: (credentials: LoginRequest, companySlug: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>({
    token: null,
    userId: null,
    companySlug: null,
    isAuthenticated: false,
    isLoading: true,
  })

  // Initialize auth state from localStorage on mount
  useEffect(() => {
    const token = getToken()
    const userId = getUserId()
    const companySlug = getCompanySlug()
    
    setState({
      token,
      userId,
      companySlug,
      isAuthenticated: !!token,
      isLoading: false,
    })
  }, [])

  const login = useCallback(async (credentials: LoginRequest, companySlug: string) => {
    // Login endpoint is at /auth/login (global, not tenant-scoped)
    const response = await api.post<LoginResponse>(
      `/auth/login`,
      credentials
    )
    
    // Store auth data
    setToken(response.token)
    setUserId(response.userId)
    setCompanySlug(companySlug)
    
    setState({
      token: response.token,
      userId: response.userId,
      companySlug,
      isAuthenticated: true,
      isLoading: false,
    })
  }, [])

  const logout = useCallback(() => {
    clearAuthData()
    
    setState({
      token: null,
      userId: null,
      companySlug: null,
      isAuthenticated: false,
      isLoading: false,
    })
  }, [])

  return (
    <AuthContext.Provider value={{ ...state, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}