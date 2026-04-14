import { useState, FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { registerUser } from '../api/authApi'
import { ApiError } from '../types/apiTypes'

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

function getRegisterErrorMessage(normalized: NormalizedError): string {
  const { status, message } = normalized

  // Network/backend unavailable
  if (status === 0) {
    return 'Backend not reachable. Check API base URL and that backend is running.'
  }

  // Conflict - email already exists
  if (status === 409) {
    return message || 'Email already registered.'
  }

  // Bad request - validation errors
  if (status === 400) {
    return message || 'Please check your input and try again.'
  }

  // Use the error message from backend or fallback
  return message || 'Unexpected error. Please try again.'
}

export function RegisterPage() {
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setSuccess(false)

    try {
      const result = await registerUser({
        email,
        password,
        // mode omitted - creates user account only (no company)
      })

      setSuccess(true)

      // Navigate to login after a brief delay to show success message
      setTimeout(() => {
        navigate('/login', { state: { registrationSuccess: true, email: result.email } })
      }, 1500)
    } catch (err: unknown) {
      const normalized = normalizeApiError(err)
      setError(getRegisterErrorMessage(normalized))
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
      <h1 style={{ marginBottom: '8px', fontSize: '24px' }}>Create Account</h1>
      <p style={{ color: '#666', marginBottom: '24px' }}>Join Venue Platform</p>

      {success && (
        <div style={{
          marginBottom: '16px',
          padding: '12px',
          background: '#dcfce7',
          color: '#166534',
          borderRadius: '4px',
          fontSize: '14px'
        }}>
          Registration successful! Redirecting to login...
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
        </div>
      )}

      <form onSubmit={handleSubmit}>
        {/* Email */}
        <div style={{ marginBottom: '16px' }}>
          <label style={{ display: 'block', marginBottom: '4px', fontSize: '14px', fontWeight: 500 }}>
            Email
          </label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
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

        {/* Password */}
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
            minLength={6}
          />
          <p style={{ marginTop: '4px', fontSize: '12px', color: '#6b7280' }}>
            Minimum 6 characters
          </p>
        </div>

        <button
          type="submit"
          disabled={loading || success}
          style={{
            width: '100%',
            padding: '10px',
            background: loading || success ? '#ccc' : '#2563eb',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            fontSize: '14px',
            fontWeight: 500,
            cursor: loading || success ? 'not-allowed' : 'pointer'
          }}
        >
          {loading ? 'Creating account...' : success ? 'Success!' : 'Create Account'}
        </button>
      </form>

      {/* Login link */}
      <div style={{ marginTop: '24px', textAlign: 'center' }}>
        <p style={{ fontSize: '14px', color: '#6b7280' }}>
          Already have an account?{' '}
          <Link
            to="/login"
            style={{
              color: '#2563eb',
              textDecoration: 'none',
              fontWeight: 500
            }}
          >
            Sign in
          </Link>
        </p>
      </div>
    </div>
  )
}
