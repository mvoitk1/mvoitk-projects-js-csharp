/**
 * =====================================================================================
 * AUTH CONTEXT - Global Authentication State Management
 * =====================================================================================
 * 
 * This file manages authentication state across the entire application using
 * React's Context API. Any component can access auth state without passing props.
 * 
 * =====================================================================================
 * KEY CONCEPTS EXPLAINED FOR STUDENTS:
 * =====================================================================================
 * 
 * 1. WHAT IS React Context?
 *    Context provides a way to pass data through the component tree without having
 *    to pass props manually at every level.
 *    
 *    Without Context:                    With Context:
 *    App -> A -> B -> C                 App (Provider)
 *    (must pass data down)               |
 *                                       All children can access directly
 *    
 *    Think of it like a bulletin board:
 *    - Anyone can post (Provider)
 *    - Anyone can read (useContext)
 *    - No need to pass notes hand-to-hand
 * 
 * 2. WHAT IS "createContext"?
 *    Creates a Context object. Components can subscribe to this context.
 *    
 *    const AuthContext = createContext<AuthContextType | null>(null)
 *    
 *    This creates the context but it has no value until wrapped with a Provider.
 * 
 * 3. WHAT IS "Context.Provider"?
 *    The Provider component accepts a "value" prop that will be available
 *    to all components that consume this context.
 *    
 *    <AuthContext.Provider value={{ token: "abc", login: fn }}>
 *      <App />
 *    </AuthContext.Provider>
 * 
 * 4. WHAT IS "useState"?
 *    useState is a React Hook that lets you add state to functional components.
 *    
 *    const [count, setCount] = useState(0)
 *    // count = current value
 *    // setCount = function to update the value
 *    
 *    When you call setCount, React re-renders the component with the new value.
 * 
 * 5. WHAT IS "useEffect"?
 *    useEffect lets you perform side effects in function components.
 *    It's like componentDidMount + componentDidUpdate + componentWillUnmount combined.
 *    
 *    useEffect(() => {
 *      // This runs after every render
 *    })
 *    
 *    useEffect(() => {
 *      // This runs ONCE when component mounts
 *    }, [])
 *    
 *    useEffect(() => {
 *      // This runs when 'dependency' changes
 *    }, [dependency])
 * 
 * 6. WHAT IS "useCallback"?
 *    useCallback returns a memoized version of a callback function.
 *    This prevents the function from being recreated on every render.
 *    
 *    WHY USE IT?
 *    If you pass a function as a prop to a child component, the child might
 *    re-render unnecessarily if the function is recreated each render.
 *    useCallback keeps the same function reference until dependencies change.
 * 
 * 7. WHAT IS "!!token" (double negation)?
 *    This converts any value to a boolean.
 *    
 *    !!token is equivalent to Boolean(token)
 *    
 *    Examples:
 *    - !!null = false
 *    - !!"" = false
 *    - !!0 = false
 *    - !!undefined = false
 *    - !!"abc" = true
 *    - !!123 = true
 * 
 * 8. WHAT IS "..." (spread operator)?
 *    The spread operator expands an iterable (like an array or object).
 *    
 *    For objects:
 *      const obj1 = { a: 1, b: 2 }
 *      const obj2 = { ...obj1, c: 3 }  // { a: 1, b: 2, c: 3 }
 *    
 *    This is useful for copying objects and overriding specific properties.
 * 
 * 9. WHAT IS "prev => ({...prev, newValue})"?
 *    This is how you update state when you need the previous value.
 *    
 *    setState(prev => ({
 *      ...prev,
 *      companySlug: newValue
 *    }))
 *    
 *    This copies all previous state and overrides just companySlug.
 * 
 * 10. WHAT IS "React.ReactNode"?
 *    Any valid React element that can be rendered. This includes:
 *    - JSX elements (<div />)
 *    - Strings ("hello")
 *    - Numbers (42)
 *    - null
 *    - Arrays of these
 * 
 * =====================================================================================
 */

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
  setCompanySlug as setStoredCompanySlug,
} from '../api/apiClient'

/**
 * AuthState - The shape of our authentication state
 * 
 * This defines what information we track about the user's login status.
 */
export interface AuthState {
  token: string | null           // JWT token (null if not logged in)
  userId: string | null          // User's ID (null if not logged in)
  companySlug: string | null     // Current company context (null if not selected)
  isAuthenticated: boolean       // Are they logged in? (derived from token)
  isLoading: boolean             // Are we still checking auth status?
}

/**
 * AuthContextType - The shape of our auth context + functions
 * 
 * This includes both the state AND functions that modify the state.
 * Components will use these functions to login/logout/switch companies.
 */
export interface AuthContextType extends AuthState {
  // Functions to modify auth state
  login: (credentials: LoginRequest, companySlug?: string) => Promise<void>
  logout: () => void
  setCompanySlug: (companySlug: string) => void
}

/**
 * Create the context with null as default
 * TypeScript knows it might be null, so we must check before using
 */
export const AuthContext = createContext<AuthContextType | null>(null)

/**
 * AuthProvider - The component that provides auth context to all children
 * 
 * This wraps your entire app (or the protected part of it).
 * Any component inside can use useAuth() to access auth state.
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  /**
   * useState - Initialize auth state
   * 
   * We start with:
   * - null values (not logged in)
   * - isLoading = true (haven't checked localStorage yet)
   */
  const [state, setState] = useState<AuthState>({
    token: null,
    userId: null,
    companySlug: null,
    isAuthenticated: false,
    isLoading: true,  // Start true - we'll set to false after checking
  })

  /**
   * useEffect - Initialize auth state from localStorage on mount
   * 
   * This runs ONCE when the component first renders (empty dependency array []).
   * It checks if there's a saved token in localStorage - if so, user is already logged in.
   */
  useEffect(() => {
    // Get saved data from localStorage
    const token = getToken()
    const userId = getUserId()
    const companySlug = getCompanySlug()
    
    // Update state with found values
    setState({
      token,
      userId,
      companySlug,
      isAuthenticated: !!token,  // Convert to boolean
      isLoading: false,           // Done loading
    })
  }, [])  // Empty array = run once on mount

  /**
   * login - Function to log a user in
   * 
   * This is wrapped in useCallback to prevent unnecessary re-renders.
   * 
   * @param credentials - Email and password
   * @param companySlug - Optional company to select after login
   */
  const login = useCallback(async (credentials: LoginRequest, companySlug?: string) => {
    // Call the login API endpoint
    const response = await api.post<LoginResponse>(
      `/auth/login`,
      credentials
    )
    
    // Save auth data to localStorage (so it persists across page refreshes)
    setToken(response.token)
    setUserId(response.userId)
    if (companySlug) {
      setStoredCompanySlug(companySlug)
    }
    
    // Update state with new values
    setState({
      token: response.token,
      userId: response.userId,
      companySlug: companySlug || null,
      isAuthenticated: true,
      isLoading: false,
    })
  }, [])  // No dependencies - this function doesn't change

  /**
   * logout - Function to log the user out
   * 
   * Clears all auth data from localStorage and resets state.
   */
  const logout = useCallback(() => {
    // Clear localStorage
    clearAuthData()
    
    // Reset state to initial values
    setState({
      token: null,
      userId: null,
      companySlug: null,
      isAuthenticated: false,
      isLoading: false,
    })
  }, [])  // No dependencies

  /**
   * setCompanySlug - Switch to a different company
   * 
   * In multi-tenant apps, users might belong to multiple companies.
   * This lets them switch between companies.
   * 
   * @param companySlug - The company to switch to
   */
  const setCompanySlug = useCallback((companySlug: string) => {
    // Save to localStorage
    setStoredCompanySlug(companySlug)
    
    // Update state - use functional update to preserve other state
    setState(prev => ({
      ...prev,           // Copy all previous state
      companySlug,      // Override companySlug with new value
    }))
  }, [])  // No dependencies

  /**
   * Provide the context to all children
   * 
   * We spread the state (...state) and add our functions (login, logout, setCompanySlug)
   */
  return (
    <AuthContext.Provider value={{ ...state, login, logout, setCompanySlug }}>
      {children}
    </AuthContext.Provider>
  )
}
