import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getSpaces, deactivateSpace } from '../api/spacesApi'
import { ApiError } from '../types/apiTypes'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function SpacesPage() {
  const companySlug = useCompanySlug()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  // Query for spaces list
  const { data: spaces, isLoading } = useQuery({
    queryKey: ['spaces', companySlug],
    queryFn: () => getSpaces(companySlug),
  })

  // Deactivate mutation
  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateSpace(companySlug, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spaces', companySlug] })
      setError(null)
    },
    onError: (err: ApiError) => {
      if (err.statusCode === 403) {
        setError('Insufficient permissions to deactivate spaces.')
      } else {
        setError(err.message || 'Failed to deactivate space.')
      }
    },
  })

  const handleDeactivate = (id: string) => {
    if (confirm('Are you sure you want to deactivate this space?')) {
      deactivateMutation.mutate(id)
    }
  }

  return (
    
      <div style={styles.container}>
        <div style={styles.header}>
          <h1 style={styles.title}>Spaces</h1>
          <button
            onClick={() => navigate(`/${companySlug}/spaces/new`)}
            style={styles.newButton}
          >
            + New Space
          </button>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        {isLoading ? (
          <div style={styles.loading}>Loading spaces...</div>
        ) : (
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Name</th>
                  <th style={styles.th}>Capacity</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {spaces?.length === 0 ? (
                  <tr>
                    <td colSpan={4} style={styles.emptyCell}>
                      No spaces yet. Create your first space above.
                    </td>
                  </tr>
                ) : (
                  spaces?.map((space) => (
                    <tr key={space.id} style={styles.tr}>
                      <td style={styles.td}>{space.name}</td>
                      <td style={styles.td}>{space.capacity}</td>
                      <td style={styles.td}>
                        <span style={space.isActive ? styles.activeBadge : styles.inactiveBadge}>
                          {space.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td style={styles.td}>
                        <div style={styles.actions}>
                          <button
                            onClick={() => navigate(`/${companySlug}/spaces/${space.id}`)}
                            style={styles.actionButton}
                          >
                            View
                          </button>
                          <button
                            onClick={() => navigate(`/${companySlug}/spaces/${space.id}/edit`)}
                            style={styles.actionButton}
                          >
                            Edit
                          </button>
                          {space.isActive && (
                            <button
                              onClick={() => handleDeactivate(space.id)}
                              style={styles.deactivateButton}
                              disabled={deactivateMutation.isPending}
                            >
                              Deactivate
                            </button>
                          )}
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
  activeBadge: {
    display: 'inline-block',
    padding: '4px 8px',
    backgroundColor: '#d4edda',
    color: '#155724',
    borderRadius: '4px',
    fontSize: '12px',
    fontWeight: '500',
  },
  inactiveBadge: {
    display: 'inline-block',
    padding: '4px 8px',
    backgroundColor: '#f8d7da',
    color: '#721c24',
    borderRadius: '4px',
    fontSize: '12px',
    fontWeight: '500',
  },
  deactivateButton: {
    padding: '6px 12px',
    backgroundColor: '#dc3545',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    fontSize: '12px',
    cursor: 'pointer',
  },
}
