import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getPublicCompanies } from '../api/publicCompaniesApi'
import { PublicCompanyDto } from '../types/apiTypes'

export function CustomerVenuesPage() {
  const navigate = useNavigate()
  const [searchQuery, setSearchQuery] = useState('')

  const { data: response, isLoading, error } = useQuery({
    queryKey: ['publicCompanies'],
    queryFn: getPublicCompanies,
  })

  const companies = response?.companies ?? []

  // Filter companies by name or slug
  const filteredCompanies = companies.filter((company: PublicCompanyDto) => {
    const query = searchQuery.toLowerCase()
    return (
      company.companyName.toLowerCase().includes(query) ||
      company.companySlug.toLowerCase().includes(query)
    )
  })

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h1 style={styles.title}>Venue Directory</h1>
        <p style={styles.subtitle}>Browse and discover venues</p>
      </div>

      <div style={styles.searchContainer}>
        <input
          type="text"
          placeholder="Search venues by name or slug..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          style={styles.searchInput}
        />
      </div>

      {isLoading ? (
        <div style={styles.loading}>Loading venues...</div>
      ) : error ? (
        <div style={styles.error}>
          Failed to load venues. Please try again.
        </div>
      ) : (
        <>
          <div style={styles.stats}>
            Showing {filteredCompanies.length} of {companies.length} venues
          </div>

          {filteredCompanies.length === 0 ? (
            <div style={styles.emptyState}>
              <p style={styles.emptyText}>
                {searchQuery
                  ? 'No venues match your search.'
                  : 'No venues available yet.'}
              </p>
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  style={styles.clearButton}
                >
                  Clear Search
                </button>
              )}
            </div>
          ) : (
            <div style={styles.grid}>
              {filteredCompanies.map((company: PublicCompanyDto) => (
                <div
                  key={company.companySlug}
                  style={styles.card}
                  onClick={() => navigate(`/customer/venues/${company.companySlug}`)}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>{company.companyName}</h3>
                    <span style={styles.planBadge}>{company.plan}</span>
                  </div>
                  <p style={styles.cardSlug}>@{company.companySlug}</p>
                  <div style={styles.cardFooter}>
                    <span style={styles.viewLink}>View Details →</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: '24px',
  },
  title: {
    fontSize: '28px',
    fontWeight: 'bold',
    color: '#333',
    margin: '0 0 8px 0',
  },
  subtitle: {
    fontSize: '16px',
    color: '#666',
    margin: 0,
  },
  searchContainer: {
    marginBottom: '24px',
  },
  searchInput: {
    width: '100%',
    maxWidth: '400px',
    padding: '12px 16px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '8px',
    outline: 'none',
  },
  stats: {
    fontSize: '14px',
    color: '#666',
    marginBottom: '16px',
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  error: {
    padding: '16px',
    backgroundColor: '#fee',
    color: '#c33',
    borderRadius: '8px',
    marginBottom: '16px',
  },
  emptyState: {
    padding: '60px 20px',
    textAlign: 'center',
    backgroundColor: '#f9fafb',
    borderRadius: '12px',
    border: '2px dashed #e5e7eb',
  },
  emptyText: {
    fontSize: '16px',
    color: '#666',
    margin: '0 0 16px 0',
  },
  clearButton: {
    padding: '8px 16px',
    fontSize: '14px',
    backgroundColor: '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '6px',
    cursor: 'pointer',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: '20px',
  },
  card: {
    backgroundColor: 'white',
    borderRadius: '12px',
    padding: '20px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    cursor: 'pointer',
    transition: 'box-shadow 0.2s, transform 0.2s',
    border: '1px solid #e5e7eb',
  },
  cardHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: '8px',
  },
  cardTitle: {
    fontSize: '18px',
    fontWeight: '600',
    color: '#333',
    margin: 0,
    flex: 1,
    wordBreak: 'break-word',
  },
  planBadge: {
    fontSize: '12px',
    fontWeight: '500',
    padding: '4px 8px',
    backgroundColor: '#dbeafe',
    color: '#1e40af',
    borderRadius: '4px',
    marginLeft: '8px',
    flexShrink: 0,
  },
  cardSlug: {
    fontSize: '14px',
    color: '#6b7280',
    margin: '0 0 16px 0',
  },
  cardFooter: {
    display: 'flex',
    justifyContent: 'flex-end',
  },
  viewLink: {
    fontSize: '14px',
    color: '#2563eb',
    fontWeight: '500',
  },
}
