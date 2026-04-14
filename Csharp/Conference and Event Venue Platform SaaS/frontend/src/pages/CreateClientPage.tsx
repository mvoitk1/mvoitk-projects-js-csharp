import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ClientForm } from '../components/clients/ClientForm'
import { createClient } from '../api/clientsApi'
import { useCompanySlug } from '../hooks/useCompanySlug'
import { useState } from 'react'
import type { CreateClientRequest } from '../types/apiTypes'

export function CreateClientPage() {
  const companySlug = useCompanySlug()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const createMutation = useMutation({
    mutationFn: (data: CreateClientRequest) => createClient(companySlug, data),
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: ['clients', companySlug] })
      const createdId = response?.id?.trim()
      if (createdId) {
        navigate(`/${companySlug}/clients/${createdId}`)
        return
      }

      navigate(`/${companySlug}/clients`, {
        state: { notice: 'Client created.' },
      })
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to create client. Please try again.')
    },
  })

  const handleSubmit = (data: CreateClientRequest) => {
    setError(null)
    createMutation.mutate(data)
  }

  return (
    
      <div style={styles.container}>
        <div style={styles.header}>
          <button
            onClick={() => navigate(`/${companySlug}/clients`)}
            style={styles.backLink}
          >
            ← Back to Clients
          </button>
          <h1 style={styles.title}>New Client</h1>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        <ClientForm
          onSubmit={handleSubmit}
          isSubmitting={createMutation.isPending}
          submitLabel="Create Client"
        />
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
}
