import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Layout } from '../components/Layout'
import { getClients } from '../api/clientsApi'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function ClientsPage() {
  const companySlug = useCompanySlug()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  // Query for clients list
  const { data: clients, isLoading, isError } = useQuery({
    queryKey: ['clients', companySlug],
    queryFn: () => getClients(companySlug),
  })

  // Show API error
  if (isError && !error) {
    setError('Failed to load clients. Please try again.')
  }

  return (
    <Layout>
      <div style={styles.container}>
        <div style={styles.header}>
          <h1 style={styles.title}>Clients</h1>
          <button
            onClick={() => navigate(`/${companySlug}/clients/new`)}
            style={styles.newButton}
          >
            + New Client
          </button>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        {isLoading ? (
          <div style={styles.loading}>Loading clients...</div>
        ) : (
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Name</th>
                  <th style={styles.th}>Email</th>
                  <th style={styles.th}>Notes</th>
                  <th style={styles.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {clients?.length === 0 ? (
                  <tr>
                    <td colSpan={4} style={styles.emptyCell}>
                      No clients yet.
                    </td>
                  </tr>
                ) : (
                  clients?.map((client) => (
                    <tr key={client.id} style={styles.tr}>
                      <td style={styles.td}>{client.name}</td>
                      <td style={styles.td}>
                        {client.email ? (
                          <a href={`mailto:${client.email}`} style={styles.emailLink}>
                            {client.email}
                          </a>
                        ) : (
                          <span style={styles.emptyValue}>—</span>
                        )}
                      </td>
                      <td style={styles.td}>
                        {client.notes || <span style={styles.emptyValue}>—</span>}
                      </td>
                      <td style={styles.td}>
                        <div style={styles.actions}>
                          <button
                            onClick={() => navigate(`/${companySlug}/clients/${client.id}`)}
                            style={styles.actionButton}
                          >
                            View
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Layout>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '1000px',
    width: '100%',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '24px',
  },
  title: {
    fontSize: '28px',
    fontWeight: 'bold',
    color: '#333',
    margin: 0,
  },
  newButton: {
    padding: '10px 20px',
    fontSize: '14px',
    fontWeight: '600',
    border: 'none',
    borderRadius: '4px',
    backgroundColor: '#007bff',
    color: 'white',
    cursor: 'pointer',
  },
  errorBanner: {
    padding: '12px 16px',
    backgroundColor: '#fee',
    color: '#c33',
    borderRadius: '4px',
    marginBottom: '16px',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  closeError: {
    background: 'none',
    border: 'none',
    fontSize: '20px',
    color: '#c33',
    cursor: 'pointer',
    padding: '0 4px',
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  tableContainer: {
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    overflow: 'hidden',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
  },
  th: {
    padding: '12px 16px',
    textAlign: 'left',
    fontSize: '14px',
    fontWeight: '600',
    color: '#666',
    borderBottom: '1px solid #e0e0e0',
    backgroundColor: '#f8f9fa',
  },
  tr: {
    borderBottom: '1px solid #e0e0e0',
  },
  td: {
    padding: '12px 16px',
    fontSize: '14px',
    color: '#333',
  },
  emptyCell: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  emailLink: {
    color: '#007bff',
    textDecoration: 'none',
  },
  emptyValue: {
    color: '#999',
    fontStyle: 'italic',
  },
  actions: {
    display: 'flex',
    gap: '8px',
  },
  actionButton: {
    padding: '6px 12px',
    fontSize: '12px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    backgroundColor: 'white',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
  },
}
