import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Layout } from '../components/Layout'
import { getClient, deleteClient } from '../api/clientsApi'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function ClientDetailsPage() {
  const companySlug = useCompanySlug()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  // Query for client details
  const { data: client, isLoading, isError, error } = useQuery({
    queryKey: ['client', companySlug, id],
    queryFn: () => getClient(companySlug, id!),
    enabled: !!id,
  })

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: () => deleteClient(companySlug, id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients', companySlug] })
      navigate(`/${companySlug}/clients`)
    },
    onError: (err: Error) => {
      setDeleteError(err.message || 'Failed to delete client. Please try again.')
    },
  })

  const formatDateTime = (utcString: string | undefined) => {
    if (!utcString) return 'N/A'
    const date = new Date(utcString)
    return date.toLocaleString()
  }

  const handleDelete = () => {
    setDeleteError(null)
    deleteMutation.mutate()
  }

  const getErrorMessage = () => {
    if (error instanceof Error) {
      return error.message
    }
    return 'Failed to load client details. Please try again.'
  }

  if (isLoading) {
    return (
      <Layout>
        <div style={styles.container}>
          <div style={styles.loading}>Loading client details...</div>
        </div>
      </Layout>
    )
  }

  if (isError || !client) {
    return (
      <Layout>
        <div style={styles.container}>
          <div style={styles.errorBanner}>{isError ? getErrorMessage() : 'Client not found'}</div>
          <button
            onClick={() => navigate(`/${companySlug}/clients`)}
            style={styles.backButton}
          >
            Back to Clients
          </button>
        </div>
      </Layout>
    )
  }

  return (
    <Layout>
      <div style={styles.container}>
        <div style={styles.header}>
          <div>
            <button
              onClick={() => navigate(`/${companySlug}/clients`)}
              style={styles.backLink}
            >
              ← Back to Clients
            </button>
            <h1 style={styles.title}>{client.name}</h1>
          </div>
          <div style={styles.headerActions}>
            <button
              onClick={() => navigate(`/${companySlug}/clients/${id}/edit`)}
              style={styles.editButton}
            >
              Edit
            </button>
            <button
              onClick={() => setShowDeleteConfirm(true)}
              style={styles.deleteButton}
            >
              Delete
            </button>
          </div>
        </div>

        {deleteError && (
          <div style={styles.errorBanner}>
            {deleteError}
            <button onClick={() => setDeleteError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        <div style={styles.detailsCard}>
          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Name</span>
            <span style={styles.detailValue}>{client.name}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Email</span>
            <span style={styles.detailValue}>
              {client.email ? (
                <a href={`mailto:${client.email}`} style={styles.emailLink}>
                  {client.email}
                </a>
              ) : (
                <span style={styles.emptyValue}>—</span>
              )}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Notes</span>
            <span style={styles.detailValue}>
              {client.notes || <span style={styles.emptyValue}>—</span>}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Created</span>
            <span style={styles.detailValue}>{formatDateTime(client.createdUtc)}</span>
          </div>
        </div>

        {/* Delete Confirmation Dialog */}
        {showDeleteConfirm && (
          <div style={styles.modalOverlay}>
            <div style={styles.modal}>
              <h3 style={styles.modalTitle}>Confirm Delete</h3>
              <p style={styles.modalText}>
                Are you sure you want to delete <strong>{client.name}</strong>?
                This action cannot be undone.
              </p>
              <div style={styles.modalActions}>
                <button
                  onClick={() => setShowDeleteConfirm(false)}
                  style={styles.cancelButton}
                  disabled={deleteMutation.isPending}
                >
                  Cancel
                </button>
                <button
                  onClick={handleDelete}
                  style={styles.confirmDeleteButton}
                  disabled={deleteMutation.isPending}
                >
                  {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Layout>
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
  deleteButton: {
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
  emailLink: {
    color: '#007bff',
    textDecoration: 'none',
  },
  emptyValue: {
    color: '#999',
    fontStyle: 'italic',
  },
  modalOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
  },
  modal: {
    backgroundColor: 'white',
    borderRadius: '8px',
    padding: '24px',
    maxWidth: '400px',
    width: '90%',
    boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
  },
  modalTitle: {
    fontSize: '18px',
    fontWeight: 'bold',
    color: '#333',
    margin: '0 0 12px 0',
  },
  modalText: {
    fontSize: '14px',
    color: '#666',
    margin: '0 0 20px 0',
    lineHeight: 1.5,
  },
  modalActions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '12px',
  },
  cancelButton: {
    padding: '10px 20px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    backgroundColor: 'white',
    cursor: 'pointer',
  },
  confirmDeleteButton: {
    padding: '10px 20px',
    fontSize: '14px',
    border: 'none',
    borderRadius: '4px',
    backgroundColor: '#dc3545',
    color: 'white',
    cursor: 'pointer',
  },
}
