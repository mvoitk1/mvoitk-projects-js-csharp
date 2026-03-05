import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getSpace, deactivateSpace } from '../api/spacesApi'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function SpaceDetailsPage() {
  const companySlug = useCompanySlug()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // Query for space details
  const { data: space, isLoading, isError, error } = useQuery({
    queryKey: ['space', companySlug, id],
    queryFn: () => getSpace(companySlug, id!),
    enabled: !!id,
  })

  // Deactivate mutation
  const deactivateMutation = useMutation({
    mutationFn: () => deactivateSpace(companySlug, id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spaces', companySlug] })
      queryClient.invalidateQueries({ queryKey: ['space', companySlug, id] })
      alert('Space has been deactivated successfully')
    },
    onError: (err: Error) => {
      alert(err.message || 'Failed to deactivate space. Please try again.')
    },
  })

  const handleDeactivate = () => {
    if (!space) return

    const confirmed = window.confirm(
      `Are you sure you want to deactivate "${space.name}"? This action cannot be undone.`
    )

    if (confirmed) {
      deactivateMutation.mutate()
    }
  }

  const getErrorMessage = () => {
    if (error instanceof Error) {
      return error.message
    }
    return 'Failed to load space details. Please try again.'
  }

  if (isLoading) {
    return (
      
        <div style={styles.container}>
          <div style={styles.loading}>Loading space details...</div>
        </div>
      
    )
  }

  if (isError || !space) {
    return (
      
        <div style={styles.container}>
          <div style={styles.errorBanner}>{isError ? getErrorMessage() : 'Space not found'}</div>
          <button
            onClick={() => navigate(`/${companySlug}/spaces`)}
            style={styles.backButton}
          >
            Back to Spaces
          </button>
        </div>
      
    )
  }

  return (
    
      <div style={styles.container}>
        <div style={styles.header}>
          <div>
            <button
              onClick={() => navigate(`/${companySlug}/spaces`)}
              style={styles.backLink}
            >
              ← Back to Spaces
            </button>
            <h1 style={styles.title}>{space.name}</h1>
          </div>
          <div style={styles.headerActions}>
            <button
              onClick={() => navigate(`/${companySlug}/spaces/${id}/edit`)}
              style={styles.editButton}
            >
              Edit
            </button>
            {space.isActive && (
              <button
                onClick={handleDeactivate}
                style={styles.deactivateButton}
                disabled={deactivateMutation.isPending}
              >
                {deactivateMutation.isPending ? 'Deactivating...' : 'Deactivate'}
              </button>
            )}
          </div>
        </div>

        <div style={styles.detailsCard}>
          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Name</span>
            <span style={styles.detailValue}>{space.name}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Capacity</span>
            <span style={styles.detailValue}>{space.capacity}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Notes</span>
            <span style={styles.detailValue}>
              {space.notes || <span style={styles.emptyValue}>No notes</span>}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Status</span>
            <span style={space.isActive ? styles.statusActive : styles.statusDeactivated}>
              {space.isActive ? 'Active' : 'Deactivated'}
            </span>
          </div>
        </div>
      </div>
    
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '800px',
    width: '100%',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: '24px',
  },
  backLink: {
    display: 'inline-block',
    marginBottom: '8px',
    color: '#007bff',
    textDecoration: 'none',
    fontSize: '14px',
    background: 'none',
    border: 'none',
    cursor: 'pointer',
    padding: 0,
  },
  title: {
    fontSize: '28px',
    fontWeight: 'bold',
    color: '#333',
    margin: 0,
  },
  headerActions: {
    display: 'flex',
    gap: '8px',
  },
  editButton: {
    padding: '8px 16px',
    fontSize: '14px',
    border: '1px solid #007bff',
    borderRadius: '4px',
    backgroundColor: 'white',
    color: '#007bff',
    cursor: 'pointer',
  },
  deactivateButton: {
    padding: '8px 16px',
    fontSize: '14px',
    border: '1px solid #dc3545',
    borderRadius: '4px',
    backgroundColor: 'white',
    color: '#dc3545',
    cursor: 'pointer',
  },
  backButton: {
    padding: '10px 20px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    backgroundColor: 'white',
    cursor: 'pointer',
    marginTop: '16px',
  },
  errorBanner: {
    padding: '12px 16px',
    backgroundColor: '#fee',
    color: '#c33',
    borderRadius: '4px',
    marginBottom: '16px',
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  detailsCard: {
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    padding: '24px',
  },
  detailRow: {
    display: 'flex',
    padding: '12px 0',
    borderBottom: '1px solid #f0f0f0',
  },
  detailLabel: {
    width: '150px',
    fontWeight: '600',
    color: '#666',
    fontSize: '14px',
  },
  detailValue: {
    flex: 1,
    color: '#333',
    fontSize: '14px',
  },
  emptyValue: {
    color: '#999',
    fontStyle: 'italic',
  },
  statusActive: {
    flex: 1,
    color: '#28a745',
    fontSize: '14px',
    fontWeight: '600',
  },
  statusDeactivated: {
    flex: 1,
    color: '#dc3545',
    fontSize: '14px',
    fontWeight: '600',
  },
}
