import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { getMe } from '../api/authApi'
import { MeResponse } from '../types/apiTypes'

export function CustomerHomePage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const [me, setMe] = useState<MeResponse | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchMe = async () => {
      try {
        const data = await getMe()
        setMe(data)
      } catch (err) {
        // If not authenticated, redirect to login
        navigate('/login')
      } finally {
        setLoading(false)
      }
    }

    fetchMe()
  }, [navigate])

  const handleLogout = () => {
    logout()
  }

  const handleGoToWorkspace = () => {
    navigate('/session')
  }

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.card}>
          <p>Loading...</p>
        </div>
      </div>
    )
  }

  const hasCompanies = me?.hasCompanies ?? false

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>Welcome</h1>
        
        {hasCompanies ? (
          <>
            <p style={styles.subtitle}>
              You are a member of {me?.companies.length} venue{me?.companies.length !== 1 ? 's' : ''}.
            </p>

            <div style={styles.actions}>
              <button
                onClick={handleGoToWorkspace}
                style={styles.primaryButton}
              >
                Go to Workspace
              </button>

              <div style={styles.divider}>
                <span style={styles.dividerText}>or</span>
              </div>

              <button
                onClick={() => navigate('/customer/venues')}
                style={styles.secondaryButtonEnabled}
              >
                Browse Venues
              </button>
            </div>
          </>
        ) : (
          <>
            <p style={styles.subtitle}>
              You are not part of any venue yet.
            </p>

            <div style={styles.actions}>
              <button
                onClick={() => navigate('/become-a-venue')}
                style={styles.primaryButton}
              >
                Become a Venue
              </button>

              <div style={styles.divider}>
                <span style={styles.dividerText}>or</span>
              </div>

              <button
                onClick={() => navigate('/customer/venues')}
                style={styles.secondaryButtonEnabled}
              >
                Browse Venues
              </button>
            </div>
          </>
        )}

        <div style={styles.footer}>
          <button onClick={handleLogout} style={styles.logoutButton}>
            Logout
          </button>
        </div>
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
    width: '100%',
    maxWidth: '400px',
    textAlign: 'center',
  },
  title: {
    fontSize: '24px',
    fontWeight: 'bold',
    marginBottom: '8px',
    color: '#333',
  },
  subtitle: {
    fontSize: '14px',
    color: '#666',
    marginBottom: '32px',
  },
  actions: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
    marginBottom: '32px',
  },
  primaryButton: {
    padding: '14px 24px',
    backgroundColor: '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '8px',
    fontSize: '16px',
    fontWeight: '500',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
  },
  secondaryButtonEnabled: {
    padding: '14px 24px',
    backgroundColor: '#f3f4f6',
    color: '#374151',
    border: '1px solid #e5e7eb',
    borderRadius: '8px',
    fontSize: '14px',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
  },
  divider: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    color: '#9ca3af',
    fontSize: '14px',
  },
  dividerText: {
    textTransform: 'uppercase',
    fontSize: '12px',
  },
  footer: {
    borderTop: '1px solid #e5e7eb',
    paddingTop: '24px',
  },
  logoutButton: {
    padding: '10px 20px',
    backgroundColor: 'transparent',
    color: '#6b7280',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '14px',
    cursor: 'pointer',
  },
}
