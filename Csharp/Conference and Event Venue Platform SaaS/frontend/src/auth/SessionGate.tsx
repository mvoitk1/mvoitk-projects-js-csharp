import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getMe } from '../api/authApi'
import { useAuth } from './useAuth'
import { ApiError } from '../types/apiTypes'

const LAST_COMPANY_SLUG_KEY = 'lastCompanySlug'

/**
 * SessionGate - Global session bootstrap component
 * 
 * Runs once after authentication to determine correct app state:
 * - No companies → /customer
 * - One company → /:companySlug/dashboard
 * - Multiple companies → checks localStorage.lastCompanySlug
 *   - If stored slug exists → redirect to that company
 *   - Otherwise → /select-company
 */
export function SessionGate() {
  const navigate = useNavigate()
  const { setCompanySlug } = useAuth()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const resolveSession = async () => {
      try {
        // Fetch current user info with company memberships
        const me = await getMe()

        // Case 1: No companies → customer view
        if (!me.hasCompanies) {
          navigate('/customer', { replace: true })
          return
        }

        // Case 2: Has companies
        if (me.companies.length === 1) {
          // Single company → go directly to dashboard
          const company = me.companies[0]
          setCompanySlug(company.companySlug)
          navigate(`/${company.companySlug}/dashboard`, { replace: true })
          return
        }

        // Multiple companies → check for last selected company
        const lastCompanySlug = localStorage.getItem(LAST_COMPANY_SLUG_KEY)
        
        if (lastCompanySlug) {
          // Check if the stored slug is still valid for this user
          const companyExists = me.companies.some(
            c => c.companySlug === lastCompanySlug
          )
          
          if (companyExists) {
            // Redirect to last selected company
            setCompanySlug(lastCompanySlug)
            navigate(`/${lastCompanySlug}/dashboard`, { replace: true })
            return
          }
        }

        // No valid last company stored → show selection page
        navigate('/select-company', { replace: true })
      } catch (err) {
        if (err instanceof ApiError && err.statusCode === 401) {
          // Not authenticated → redirect to login
          navigate('/login', { replace: true })
        } else {
          setError('Failed to load session. Please try again.')
        }
      }
    }

    resolveSession()
  }, [navigate, setCompanySlug])

  if (error) {
    return (
      <div style={styles.container}>
        <div style={styles.card}>
          <p style={styles.error}>{error}</p>
          <button 
            onClick={() => window.location.reload()} 
            style={styles.button}
          >
            Retry
          </button>
        </div>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <div style={styles.spinner} />
        <p style={styles.text}>Loading your workspace...</p>
      </div>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#f5f5f5',
    padding: '20px',
  },
  card: {
    backgroundColor: 'white',
    padding: '40px',
    borderRadius: '8px',
    boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '16px',
  },
  spinner: {
    width: '40px',
    height: '40px',
    border: '3px solid #e5e7eb',
    borderTop: '3px solid #2563eb',
    borderRadius: '50%',
    animation: 'spin 1s linear infinite',
  },
  text: {
    fontSize: '16px',
    color: '#6b7280',
    margin: 0,
  },
  error: {
    color: '#dc2626',
    marginBottom: '16px',
  },
  button: {
    padding: '10px 20px',
    backgroundColor: '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    fontSize: '14px',
    cursor: 'pointer',
  },
}

// Add keyframes for spinner animation
const styleSheet = document.createElement('style')
styleSheet.textContent = `
  @keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
  }
`
document.head.appendChild(styleSheet)
