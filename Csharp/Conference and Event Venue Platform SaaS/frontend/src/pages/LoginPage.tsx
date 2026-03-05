import { useState, FormEvent, useEffect } from 'react'
import { useNavigate, Link, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ApiError } from '../types/apiTypes'
import { loginUser } from '../api/authApi'

interface NormalizedError {
  status: number
  code: string | null
  message: string
  details?: Record<string, string[]>
}

function normalizeApiError(err: unknown): NormalizedError {
  if (err instanceof ApiError) {
    return {
      status: err.statusCode ?? 0,
      code: err.code,
      message: err.message,
      details: err.details,
    }
  }
  
  if (err instanceof Error) {
    return {
      status: 0,
      code: 'network_error',
      message: err.message,
    }
  }
  
  return {
    status: 0,
    code: 'unknown_error',
    message: 'An unexpected error occurred',
  }
}

function getLoginErrorMessage(normalized: NormalizedError): string {
  const { status, code } = normalized
  
  // Network/backend unavailable
  if (status === 0) {
    return 'Backend not reachable. Check API base URL and that backend is running.'
  }
  
  // Company not found (from ProtectedRoute or tenant resolution)
  if (status === 404) {
    return 'Company not found. Check company slug.'
  }
  
  // Invalid credentials
  if (status === 401 && code === 'invalid_credentials') {
    return 'Invalid email or password.'
  }
  
  // General unauthorized (token expired, etc)
  if (status === 401) {
    return 'Session expired. Please log in again.'
  }
  
  // Forbidden - membership issue
  if (status === 403) {
    return 'You do not have access to this company.'
  }
  
  // Use the error message from backend or fallback
  return normalized.message || 'Unexpected error. Please try again.'
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login } = useAuth()

  const [companySlug, setCompanySlug] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [normalizedError, setNormalizedError] = useState<NormalizedError | null>(null)
  const [showDetails, setShowDetails] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const isDev = import.meta.env.DEV

  // Handle state passed from registration
  useEffect(() => {
    const state = location.state as { registeredEmail?: string; companySlug?: string } | null
    if (state?.registeredEmail) {
      setEmail(state.registeredEmail)
      setSuccessMessage('Account created successfully! Please sign in.')
      if (state.companySlug) {
        setCompanySlug(state.companySlug)
      }
      // Clear state so message doesn't persist on refresh
      window.history.replaceState({}, document.title)
    }
  }, [location.state])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setNormalizedError(null)
    setShowDetails(false)
    
    try {
      const result = await loginUser({
        email,
        password,
      })
      
      if (!result || !result.token) {
        throw new Error('Login failed: invalid response')
      }
      
      // Store token via AuthContext
      login({ email, password }, companySlug.toLowerCase())
      
      // Navigate to company dashboard
      navigate(`/${companySlug.toLowerCase()}/dashboard`)
    } catch (err: unknown) {
      const normalized = normalizeApiError(err)
      setNormalizedError(normalized)
      setError(getLoginErrorMessage(normalized))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ 
      maxWidth: '400px', 
      margin: '80px auto', 
      padding: '24px',
      background: 'white',
      borderRadius: '8px',
      boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
    }}>
      <h1 style={{ marginBottom: '8px', fontSize: '24px' }}>Venue Platform</h1>
      <p style={{ color: '#666', marginBottom: '24px' }}>Sign in to your company workspace</p>

      {successMessage && (
        <div style={{
          marginBottom: '16px',
          padding: '12px',
          background: '#dcfce7',
          color: '#166534',
          borderRadius: '4px',
          fontSize: '14px'
        }}>
          {successMessage}
        </div>
      )}

      {error && (
        <div style={{ 
          marginBottom: '16px', 
          padding: '12px', 
          background: '#fee2e2', 
          color: '#991b1b',
          borderRadius: '4px',
          fontSize: '14px'
        }}>
          {error}
          
          {/* Dev-only error details */}
          {isDev && normalizedError && (
            <div style={{ marginTop: '8px' }}>
              <button
                type="button"
                onClick={() => setShowDetails(!showDetails)}
                style={{
                  fontSize: '12px',
                  color: '#991b1b',
                  textDecoration: 'underline',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  padding: 0,
                }}
              >
                {showDetails ? 'Hide Details' : 'Show Details'}
              </button>
              
              {showDetails && (
                <pre style={{ 
                  marginTop: '8px',
                  padding: '8px',
                  background: '#fef2f2',
                  borderRadius: '4px',
                  fontSize: '11px',
                  overflow: 'auto',
                  maxHeight: '200px'
                }}>
                  {JSON.stringify(normalizedError, null, 2)}
                </pre>
              )}
            </div>
          )}
        </div>
      )}
      
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '16px' }}>
          <label style={{ display: 'block', marginBottom: '4px', fontSize: '14px', fontWeight: 500 }}>
            Company Slug
          </label>
          <input
            type="text"
            value={companySlug}
            onChange={(e) => setCompanySlug(e.target.value)}
            placeholder="your-company"
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ddd',
              borderRadius: '4px',
              fontSize: '14px',
              boxSizing: 'border-box'
            }}
            required
          />
        </div>
        
        <div style={{ marginBottom: '16px' }}>
          <label style={{ display: 'block', marginBottom: '4px', fontSize: '14px', fontWeight: 500 }}>
            Email
          </label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@company.com"
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ddd',
              borderRadius: '4px',
              fontSize: '14px',
              boxSizing: 'border-box'
            }}
            required
          />
        </div>
        
        <div style={{ marginBottom: '24px' }}>
          <label style={{ display: 'block', marginBottom: '4px', fontSize: '14px', fontWeight: 500 }}>
            Password
          </label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ddd',
              borderRadius: '4px',
              fontSize: '14px',
              boxSizing: 'border-box'
            }}
            required
          />
        </div>
        
        <button
          type="submit"
          disabled={loading}
          style={{
            width: '100%',
            padding: '10px',
            background: loading ? '#ccc' : '#2563eb',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            fontSize: '14px',
            fontWeight: 500,
            cursor: loading ? 'not-allowed' : 'pointer'
          }}
        >
          {loading ? 'Signing in...' : 'Sign In'}
        </button>
      </form>
      
      {/* Register link */}
      <div style={{ marginTop: '24px', textAlign: 'center' }}>
        <p style={{ fontSize: '14px', color: '#6b7280' }}>
          Don&apos;t have an account?{' '}
          <Link
            to="/register"
            style={{
              color: '#2563eb',
              textDecoration: 'none',
              fontWeight: 500
            }}
          >
            Create an account
          </Link>
        </p>
      </div>

      {/* Dev-only bootstrap link */}
      {isDev && (
        <div style={{ marginTop: '16px', textAlign: 'center' }}>
          <Link
            to="/dev/bootstrap"
            style={{
              fontSize: '12px',
              color: '#666',
              textDecoration: 'underline'
            }}
          >
            Dev bootstrap
          </Link>
        </div>
      )}
    </div>
  )
}
