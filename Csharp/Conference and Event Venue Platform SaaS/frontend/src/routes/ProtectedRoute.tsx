import { Navigate, useParams, Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../auth/useAuth'
import { api } from '../api/apiClient'
import { ApiError } from '../types/apiTypes'

interface ProtectedRouteProps {
  children: React.ReactNode
}

type MembershipStatus = 'loading' | 'valid' | 'no_access' | 'not_found' | 'error'

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading: authLoading, companySlug: storedCompanySlug } = useAuth()
  const { companySlug } = useParams<{ companySlug: string }>()
  const [membershipStatus, setMembershipStatus] = useState<MembershipStatus>('loading')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  
  useEffect(() => {
    // Only check membership when authenticated and company slug is available
    if (!isAuthenticated || authLoading) return
    
    const checkMembership = async () => {
      setMembershipStatus('loading')
      
      try {
        // Verify tenant membership by calling billing/plan endpoint
        await api.get(`/${companySlug}/billing/plan`)
        setMembershipStatus('valid')
      } catch (err) {
        if (err instanceof ApiError) {
          switch (err.statusCode) {
            case 403:
              setMembershipStatus('no_access')
              setErrorMessage('You do not have access to this company.')
              break
            case 404:
              setMembershipStatus('not_found')
              setErrorMessage('Company not found.')
              break
            default:
              setMembershipStatus('error')
              setErrorMessage(err.message || 'An error occurred')
          }
        } else {
          setMembershipStatus('error')
          setErrorMessage('An unexpected error occurred')
        }
      }
    }
    
    checkMembership()
  }, [isAuthenticated, authLoading, companySlug])
  
  if (authLoading || (isAuthenticated && membershipStatus === 'loading')) {
    return <div>Loading...</div>
  }
  
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }
  
  // Ensure user is accessing their own company's routes
  if (companySlug && storedCompanySlug && companySlug !== storedCompanySlug) {
    return <Navigate to={`/${storedCompanySlug}/dashboard`} replace />
  }
  
  // Show error state for membership issues
  if (membershipStatus === 'no_access' || membershipStatus === 'not_found' || membershipStatus === 'error') {
    return (
      <div style={styles.container}>
        <div style={styles.card}>
          <h1 style={styles.title}>Access Denied</h1>
          <p style={styles.message}>{errorMessage}</p>
          <Link to="/login" style={styles.button}>
            Back to Login
          </Link>
        </div>
      </div>
    )
  }
  
  return <>{children}</>
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
    width: '100%',
    maxWidth: '400px',
    textAlign: 'center',
  },
  title: {
    fontSize: '24px',
    fontWeight: 'bold',
    marginBottom: '16px',
    color: '#333',
  },
  message: {
    fontSize: '16px',
    color: '#666',
    marginBottom: '24px',
  },
  button: {
    display: 'inline-block',
    padding: '12px 24px',
    backgroundColor: '#007bff',
    color: 'white',
    textDecoration: 'none',
    borderRadius: '4px',
    fontSize: '14px',
    fontWeight: '500',
  },
}
