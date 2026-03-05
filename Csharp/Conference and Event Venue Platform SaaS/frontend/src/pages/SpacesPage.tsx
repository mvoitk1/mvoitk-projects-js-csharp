import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Layout } from '../components/Layout'
import { getSpaces, createSpace, deactivateSpace } from '../api/spacesApi'
import { ApiError, CreateSpaceRequest } from '../types/apiTypes'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function SpacesPage() {
  const companySlug = useCompanySlug()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [showCreateForm, setShowCreateForm] = useState(false)

  // Form state
  const [createForm, setCreateForm] = useState<CreateSpaceRequest>({
    name: '',
    capacity: 0,
    hourlyRate: 0,
  })

  // Query for spaces list
  const { data: spaces, isLoading } = useQuery({
    queryKey: ['spaces', companySlug],
    queryFn: () => getSpaces(companySlug),
  })

  // Create mutation
  const createMutation = useMutation({
    mutationFn: (payload: CreateSpaceRequest) =>
      createSpace(companySlug, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spaces', companySlug] })
      setShowCreateForm(false)
      setCreateForm({ name: '', capacity: 0, hourlyRate: 0 })
      setError(null)
    },
    onError: (err: ApiError) => {
      if (err.statusCode === 409) {
        setError('Space limit reached for current plan.')
      } else if (err.statusCode === 403) {
        setError('Insufficient permissions to create spaces.')
      } else {
        setError(err.message || 'Failed to create space.')
      }
    },
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

  const handleCreateSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!createForm.name.trim()) {
      setError('Name is required.')
      return
    }
    if (createForm.capacity <= 0) {
      setError('Capacity must be greater than 0.')
      return
    }
    if (createForm.hourlyRate < 0) {
      setError('Hourly rate cannot be negative.')
      return
    }
    createMutation.mutate(createForm)
  }

  const handleDeactivate = (id: string) => {
    if (confirm('Are you sure you want to deactivate this space?')) {
      deactivateMutation.mutate(id)
    }
  }

  return (
    <Layout>
      <div style={styles.container}>
        <div style={styles.header}>
          <h1 style={styles.title}>Spaces</h1>
          <button
            onClick={() => setShowCreateForm(!showCreateForm)}
            style={styles.createButton}
          >
            {showCreateForm ? 'Cancel' : 'Create Space'}
          </button>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        {showCreateForm && (
          <form onSubmit={handleCreateSubmit} style={styles.form}>
            <h3 style={styles.formTitle}>Create New Space</h3>
            <div style={styles.formRow}>
              <label style={styles.label}>
                Name
                <input
                  type="text"
                  value={createForm.name}
                  onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })}
                  style={styles.input}
                  placeholder="Space name"
                />
              </label>
              <label style={styles.label}>
                Capacity
                <input
                  type="number"
                  value={createForm.capacity || ''}
                  onChange={(e) => setCreateForm({ ...createForm, capacity: parseInt(e.target.value) || 0 })}
                  style={styles.input}
                  min="1"
                />
              </label>
              <label style={styles.label}>
                Hourly Rate
                <input
                  type="number"
                  value={createForm.hourlyRate || ''}
                  onChange={(e) => setCreateForm({ ...createForm, hourlyRate: parseFloat(e.target.value) || 0 })}
                  style={styles.input}
                  min="0"
                  step="0.01"
                />
              </label>
            </div>
            <button
              type="submit"
              style={styles.submitButton}
              disabled={createMutation.isPending}
            >
              {createMutation.isPending ? 'Creating...' : 'Create Space'}
            </button>
          </form>
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
                  <th style={styles.th}>Hourly Rate</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {spaces?.length === 0 ? (
                  <tr>
                    <td colSpan={5} style={styles.emptyCell}>
                      No spaces yet. Create your first space above.
                    </td>
                  </tr>
                ) : (
                  spaces?.map((space) => (
                    <tr key={space.id} style={styles.tr}>
                      <td style={styles.td}>{space.name}</td>
                      <td style={styles.td}>{space.capacity}</td>
                      <td style={styles.td}>${space.hourlyRate.toFixed(2)}</td>
                      <td style={styles.td}>
                        <span style={space.isActive ? styles.activeBadge : styles.inactiveBadge}>
                          {space.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td style={styles.td}>
                        {space.isActive && (
                          <button
                            onClick={() => handleDeactivate(space.id)}
                            style={styles.deactivateButton}
                            disabled={deactivateMutation.isPending}
                          >
                            Deactivate
                          </button>
                        )}
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
  createButton: {
    padding: '10px 20px',
    backgroundColor: '#007bff',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    fontSize: '14px',
    fontWeight: '500',
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
  form: {
    backgroundColor: 'white',
    padding: '20px',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    marginBottom: '24px',
  },
  formTitle: {
    fontSize: '16px',
    fontWeight: '600',
    marginBottom: '16px',
    marginTop: 0,
    color: '#333',
  },
  formRow: {
    display: 'flex',
    gap: '16px',
    marginBottom: '16px',
    flexWrap: 'wrap',
  },
  label: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    fontSize: '14px',
    color: '#666',
    flex: '1',
    minWidth: '150px',
  },
  input: {
    padding: '8px 12px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    fontSize: '14px',
  },
  submitButton: {
    padding: '10px 20px',
    backgroundColor: '#28a745',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    fontSize: '14px',
    fontWeight: '500',
    cursor: 'pointer',
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
