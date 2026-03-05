import { useState, FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { createCompany } from '../api/companiesApi'
import { ApiError } from '../types/apiTypes'

export function BecomeVenuePage() {
  const navigate = useNavigate()
  const [companyName, setCompanyName] = useState('')
  const [companySlug, setCompanySlug] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const generateSlug = (name: string) => {
    return name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
  }

  const handleCompanyNameChange = (value: string) => {
    setCompanyName(value)
    // Auto-generate slug if slug hasn't been manually modified
    if (companySlug === generateSlug(companyName)) {
      setCompanySlug(generateSlug(value))
    }
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setSuccess(false)

    try {
      const result = await createCompany({
        name: companyName.trim(),
        slug: companySlug.toLowerCase().trim(),
      })

      setSuccess(true)
      
      // Redirect to the new company dashboard after a brief delay
      setTimeout(() => {
        navigate(`/${result.companySlug}/dashboard`)
      }, 1000)
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.statusCode === 409) {
          setError('Company slug already exists. Please choose a different one.')
        } else if (err.statusCode === 400) {
          setError(err.message || 'Please check your input and try again.')
        } else if (err.statusCode === 401) {
          setError('Your session has expired. Please log in again.')
        } else {
          setError(err.message || 'Failed to create company. Please try again.')
        }
      } else {
        setError('An unexpected error occurred. Please try again.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>Become a Venue</h1>
        <p style={styles.subtitle}>
          Create a company workspace to start managing your venue.
        </p>

        {success && (
          <div style={styles.success}>
            Company created successfully! Redirecting to dashboard...
          </div>
        )}

        {error && (
          <div style={styles.error}>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div style={styles.formGroup}>
            <label style={styles.label}>
              Company Name
            </label>
            <input
              type="text"
              value={companyName}
              onChange={(e) => handleCompanyNameChange(e.target.value)}
              placeholder="Acme Venue"
              style={styles.input}
              required
              disabled={loading || success}
            />
          </div>

          <div style={styles.formGroup}>
            <label style={styles.label}>
              Company Slug
            </label>
            <input
              type="text"
              value={companySlug}
              onChange={(e) => setCompanySlug(e.target.value.toLowerCase())}
              placeholder="acme-venue"
              style={styles.input}
              required
              disabled={loading || success}
            />
            <p style={styles.hint}>
              Used in URLs: venueplatform.com/{companySlug || 'your-slug'}
            </p>
          </div>

          <div style={styles.actions}>
            <button
              type="submit"
              disabled={loading || success}
              style={{
                ...styles.primaryButton,
                opacity: loading || success ? 0.7 : 1,
                cursor: loading || success ? 'not-allowed' : 'pointer',
              }}
            >
              {loading ? 'Creating...' : success ? 'Created!' : 'Create Company'}
            </button>
            
            <button
              type="button"
              onClick={() => navigate('/customer')}
              disabled={loading || success}
              style={styles.secondaryButton}
            >
              Cancel
            </button>
          </div>
        </form>
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
  formGroup: {
    marginBottom: '20px',
  },
  label: {
    display: 'block',
    marginBottom: '6px',
    fontSize: '14px',
    fontWeight: 500,
    color: '#374151',
  },
  input: {
    width: '100%',
    padding: '10px 12px',
    border: '1px solid #d1d5db',
    borderRadius: '6px',
    fontSize: '14px',
    boxSizing: 'border-box',
  },
  hint: {
    marginTop: '6px',
    fontSize: '12px',
    color: '#6b7280',
  },
  actions: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    marginTop: '24px',
  },
  primaryButton: {
    padding: '12px 24px',
    backgroundColor: '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '6px',
    fontSize: '16px',
    fontWeight: '500',
  },
  secondaryButton: {
    padding: '10px 24px',
    backgroundColor: 'transparent',
    color: '#6b7280',
    border: '1px solid #d1d5db',
    borderRadius: '6px',
    fontSize: '14px',
    cursor: 'pointer',
  },
  success: {
    padding: '12px',
    backgroundColor: '#dcfce7',
    color: '#166534',
    borderRadius: '6px',
    fontSize: '14px',
    marginBottom: '20px',
  },
  error: {
    padding: '12px',
    backgroundColor: '#fee2e2',
    color: '#991b1b',
    borderRadius: '6px',
    fontSize: '14px',
    marginBottom: '20px',
  },
}
