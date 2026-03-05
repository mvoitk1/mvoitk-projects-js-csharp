import { useNavigate, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getPublicCompanies } from '../api/publicCompaniesApi'
import { PublicCompanyDto } from '../types/apiTypes'

export function CustomerVenueDetailsPage() {
  const navigate = useNavigate()
  const { companySlug } = useParams<{ companySlug: string }>()

  const { data: response, isLoading, error } = useQuery({
    queryKey: ['publicCompanies'],
    queryFn: getPublicCompanies,
  })

  // Find the company from the cached list
  const company = response?.companies.find(
    (c: PublicCompanyDto) => c.companySlug === companySlug
  )

  if (isLoading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading venue details...</div>
      </div>
    )
  }

  if (error || !company) {
    return (
      <div style={styles.container}>
        <div style={styles.error}>
          <h2 style={styles.errorTitle}>Venue Not Found</h2>
          <p style={styles.errorText}>
            The venue you are looking for does not exist or has been removed.
          </p>
          <button
            onClick={() => navigate('/customer/venues')}
            style={styles.backButton}
          >
            ← Back to Venues
          </button>
        </div>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <button
        onClick={() => navigate('/customer/venues')}
        style={styles.backLink}
      >
        ← Back to Venues
      </button>

      <div style={styles.header}>
        <h1 style={styles.title}>{company.companyName}</h1>
        <span style={styles.planBadge}>{company.plan}</span>
      </div>

      <div style={styles.card}>
        <div style={styles.section}>
          <h2 style={styles.sectionTitle}>Venue Information</h2>
          <div style={styles.infoGrid}>
            <div style={styles.infoItem}>
              <span style={styles.infoLabel}>Name</span>
              <span style={styles.infoValue}>{company.companyName}</span>
            </div>
            <div style={styles.infoItem}>
              <span style={styles.infoLabel}>Slug</span>
              <span style={styles.infoValue}>@{company.companySlug}</span>
            </div>
            <div style={styles.infoItem}>
              <span style={styles.infoLabel}>Plan</span>
              <span style={styles.infoValue}>{company.plan}</span>
            </div>
          </div>
        </div>

        <div style={styles.divider} />

        <div style={styles.section}>
          <h2 style={styles.sectionTitle}>Booking</h2>
          <p style={styles.comingSoon}>
            Request booking functionality coming soon!
          </p>
          <button
            style={styles.bookingButton}
            disabled
            title="Booking feature coming soon"
          >
            Request Booking (Coming Soon)
          </button>
        </div>
      </div>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '800px',
    margin: '0 auto',
  },
  backLink: {
    padding: '8px 0',
    fontSize: '14px',
    color: '#2563eb',
    backgroundColor: 'transparent',
    border: 'none',
    cursor: 'pointer',
    marginBottom: '16px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: '16px',
    marginBottom: '24px',
  },
  title: {
    fontSize: '32px',
    fontWeight: 'bold',
    color: '#333',
    margin: 0,
  },
  planBadge: {
    fontSize: '14px',
    fontWeight: '500',
    padding: '6px 12px',
    backgroundColor: '#dbeafe',
    color: '#1e40af',
    borderRadius: '6px',
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  error: {
    padding: '40px',
    textAlign: 'center',
    backgroundColor: '#f9fafb',
    borderRadius: '12px',
    border: '2px dashed #e5e7eb',
  },
  errorTitle: {
    fontSize: '24px',
    fontWeight: 'bold',
    color: '#333',
    margin: '0 0 8px 0',
  },
  errorText: {
    fontSize: '16px',
    color: '#666',
    margin: '0 0 24px 0',
  },
  backButton: {
    padding: '10px 20px',
    fontSize: '14px',
    backgroundColor: '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '6px',
    cursor: 'pointer',
  },
  card: {
    backgroundColor: 'white',
    borderRadius: '12px',
    padding: '24px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    border: '1px solid #e5e7eb',
  },
  section: {
    marginBottom: '24px',
  },
  sectionTitle: {
    fontSize: '18px',
    fontWeight: '600',
    color: '#333',
    margin: '0 0 16px 0',
  },
  infoGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '16px',
  },
  infoItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  infoLabel: {
    fontSize: '12px',
    fontWeight: '500',
    color: '#6b7280',
    textTransform: 'uppercase',
  },
  infoValue: {
    fontSize: '16px',
    color: '#333',
  },
  divider: {
    height: '1px',
    backgroundColor: '#e5e7eb',
    margin: '24px 0',
  },
  comingSoon: {
    fontSize: '14px',
    color: '#6b7280',
    margin: '0 0 16px 0',
  },
  bookingButton: {
    padding: '12px 24px',
    fontSize: '14px',
    fontWeight: '500',
    backgroundColor: '#f3f4f6',
    color: '#9ca3af',
    border: '1px solid #e5e7eb',
    borderRadius: '6px',
    cursor: 'not-allowed',
  },
}
