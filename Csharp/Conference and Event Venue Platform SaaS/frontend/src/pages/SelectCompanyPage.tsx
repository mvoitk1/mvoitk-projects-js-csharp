import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getMe } from '../api/authApi'
import { MeResponse } from '../types/apiTypes'
import { ApiError } from '../types/apiTypes'
import { useAuth } from '../auth/useAuth'

export function SelectCompanyPage() {
  const navigate = useNavigate()
  const { setCompanySlug } = useAuth()
  const [me, setMe] = useState<MeResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchMe = async () => {
      try {
        const data = await getMe()
        setMe(data)
        
        // If no companies, redirect to customer view
        if (!data.hasCompanies) {
          navigate('/customer')
          return
        }
        
        // If only one company, redirect to it directly
        if (data.companies.length === 1) {
          navigate(`/${data.companies[0].companySlug}/dashboard`)
          return
        }
      } catch (err) {
        if (err instanceof ApiError && err.statusCode === 401) {
          navigate('/login')
        } else {
          setError('Failed to load companies. Please try again.')
        }
      } finally {
        setLoading(false)
      }
    }

    fetchMe()
  }, [navigate])

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.card}>
          <p>Loading companies...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div style={styles.container}>
        <div style={styles.card}>
          <p style={styles.error}>{error}</p>
          <button onClick={() => window.location.reload()} style={styles.button}>
            Retry
          </button>
        </div>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>Select Company</h1>
        <p style={styles.subtitle}>
          You have access to multiple companies. Choose one to continue.
        </p>

        <div style={styles.companyList}>
          {me?.companies.map((company) => (
            <button
              key={company.companySlug}
              onClick={() => {
                // Store last selected company for future sessions
                localStorage.setItem('lastCompanySlug', company.companySlug)
                setCompanySlug(company.companySlug)
                navigate(`/${company.companySlug}/dashboard`)
              }}
              style={styles.companyCard}
            >
              <div style={styles.companyName}>{company.companyName}</div>
              <div style={styles.companySlug}>/{company.companySlug}</div>
              <div style={styles.companyRole}>{company.role}</div>
            </button>
          ))}
        </div>

        <div style={styles.footer}>
          <button
            onClick={() => navigate('/customer')}
            style={styles.secondaryButton}
          >
            Back to Customer View
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
    maxWidth: '500px',
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
    marginBottom: '24px',
  },
  companyList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    marginBottom: '24px',
  },
  companyCard: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-start',
    padding: '16px',
    backgroundColor: '#f9fafb',
    border: '1px solid #e5e7eb',
    borderRadius: '8px',
    cursor: 'pointer',
    textAlign: 'left',
    transition: 'all 0.2s',
  },
  companyName: {
    fontSize: '16px',
    fontWeight: '600',
    color: '#111827',
    marginBottom: '4px',
  },
  companySlug: {
    fontSize: '13px',
    color: '#6b7280',
    marginBottom: '4px',
  },
  companyRole: {
    fontSize: '12px',
    color: '#2563eb',
    fontWeight: '500',
  },
  footer: {
    display: 'flex',
    justifyContent: 'center',
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
  secondaryButton: {
    padding: '8px 16px',
    backgroundColor: 'transparent',
    color: '#6b7280',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '14px',
    cursor: 'pointer',
  },
  error: {
    color: '#dc2626',
    marginBottom: '16px',
  },
}
