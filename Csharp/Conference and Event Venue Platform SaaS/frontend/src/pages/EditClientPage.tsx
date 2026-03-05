import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ClientForm } from '../components/clients/ClientForm'
import { getClient, updateClient } from '../api/clientsApi'
import { useCompanySlug } from '../hooks/useCompanySlug'
import type { UpdateClientRequest } from '../types/apiTypes'

export function EditClientPage() {
  const companySlug = useCompanySlug()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  // Query for client details
  const { data: client, isLoading, isError } = useQuery({
    queryKey: ['client', companySlug, id],
    queryFn: () => getClient(companySlug, id!),
    enabled: !!id,
  })

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: (data: UpdateClientRequest) => updateClient(companySlug, id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['client', companySlug, id] })
      queryClient.invalidateQueries({ queryKey: ['clients', companySlug] })
      navigate(`/${companySlug}/clients/${id}`)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to update client. Please try again.')
    },
  })

  const handleSubmit = (data: UpdateClientRequest) => {
    setError(null)
    updateMutation.mutate(data)
  }

  const handleCancel = () => {
    navigate(`/${companySlug}/clients/${id}`)
  }

  if (isLoading) {
    return (
      
        <div style={styles.container}>
          <div style={styles.loading}>Loading client...</div>
        </div>
      
    )
  }

  if (isError || !client) {
    return (
      
        <div style={styles.container}>
          <div style={styles.errorBanner}>
            Failed to load client. Please try again.
          </div>
          <button
            onClick={() => navigate(`/${companySlug}/clients`)}
            style={styles.backButton}
          >
            Back to Clients
          </button>
        </div>
      
    )
  }

  return (
    
      <div style={styles.container}>
        <div style={styles.header}>
          <div>
            <button
              onClick={() => navigate(`/${companySlug}/clients/${id}`)}
              style={styles.backLink}
            >
              ← Back to Client
            </button>
            <h1 style={styles.title}>Edit Client</h1>
          </div>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        <ClientForm
          initialValue={client}
          onSubmit={handleSubmit}
          isSubmitting={updateMutation.isPending}
          submitLabel="Save Changes"
        />

        <div style={styles.cancelSection}>
          <button
            onClick={handleCancel}
            style={styles.cancelLink}
          >
            Cancel and return to client details
          </button>
        </div>
      </div>
    
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '600px',
    width: '100%',
  },
  header: {
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
  backButton: {
    padding: '10px 20px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    backgroundColor: 'white',
    cursor: 'pointer',
    marginTop: '16px',
  },
  cancelSection: {
    marginTop: '16px',
    textAlign: 'center',
  },
  cancelLink: {
    color: '#666',
    textDecoration: 'none',
    fontSize: '14px',
    background: 'none',
    border: 'none',
    cursor: 'pointer',
    padding: 0,
  },
}
