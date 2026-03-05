import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Layout } from '../components/Layout'
import { SpaceForm } from '../components/spaces/SpaceForm'
import { createSpace } from '../api/spacesApi'
import { useCompanySlug } from '../hooks/useCompanySlug'
import { useState } from 'react'
import type { CreateSpaceRequest } from '../types/apiTypes'

export function CreateSpacePage() {
  const companySlug = useCompanySlug()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const createMutation = useMutation({
    mutationFn: (data: CreateSpaceRequest) => createSpace(companySlug, data),
    onSuccess: (space) => {
      queryClient.invalidateQueries({ queryKey: ['spaces', companySlug] })
      navigate(`/${companySlug}/spaces/${space.id}`)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to create space. Please try again.')
    },
  })

  const handleSubmit = (data: CreateSpaceRequest) => {
    setError(null)
    createMutation.mutate(data)
  }

  return (
    <Layout>
      <div style={styles.container}>
        <div style={styles.header}>
          <button
            onClick={() => navigate(`/${companySlug}/spaces`)}
            style={styles.backLink}
          >
            ← Back to Spaces
          </button>
          <h1 style={styles.title}>Create Space</h1>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        <SpaceForm
          onSubmit={handleSubmit}
          isSubmitting={createMutation.isPending}
          submitLabel="Create Space"
        />
      </div>
    </Layout>
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
