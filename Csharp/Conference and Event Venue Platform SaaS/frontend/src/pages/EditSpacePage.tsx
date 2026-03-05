import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Layout } from '../components/Layout'
import { SpaceForm } from '../components/spaces/SpaceForm'
import { getSpace, updateSpace } from '../api/spacesApi'
import { useCompanySlug } from '../hooks/useCompanySlug'
import type { UpdateSpaceRequest } from '../types/apiTypes'

export function EditSpacePage() {
  const companySlug = useCompanySlug()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  // Query for space details
  const { data: space, isLoading, isError } = useQuery({
    queryKey: ['space', companySlug, id],
    queryFn: () => getSpace(companySlug, id!),
    enabled: !!id,
  })

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: (data: UpdateSpaceRequest) => updateSpace(companySlug, id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spaces', companySlug] })
      queryClient.invalidateQueries({ queryKey: ['space', companySlug, id] })
      navigate(`/${companySlug}/spaces/${id}`)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to update space. Please try again.')
    },
  })

  const handleSubmit = (data: UpdateSpaceRequest) => {
    setError(null)
    updateMutation.mutate(data)
  }

  const handleCancel = () => {
    navigate(`/${companySlug}/spaces/${id}`)
  }

  if (isLoading) {
    return (
      <Layout>
        <div style={styles.container}>
          <div style={styles.loading}>Loading space...</div>
        </div>
      </Layout>
    )
  }

  if (isError || !space) {
    return (
      <Layout>
        <div style={styles.container}>
          <div style={styles.errorBanner}>
            Failed to load space. Please try again.
          </div>
          <button
            onClick={() => navigate(`/${companySlug}/spaces`)}
            style={styles.backButton}
          >
            Back to Spaces
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
            <Link to={`/${companySlug}/spaces/${id}`} style={styles.backLink}>
              ← Back to Space
            </Link>
            <h1 style={styles.title}>Edit Space</h1>
          </div>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        <SpaceForm
          initialValue={{
            name: space.name,
            capacity: space.capacity,
            notes: space.notes || '',
          }}
          onSubmit={handleSubmit}
          isSubmitting={updateMutation.isPending}
          submitLabel="Save Changes"
        />

        <div style={styles.cancelSection}>
          <button
            onClick={handleCancel}
            style={styles.cancelLink}
          >
            Cancel and return to space details
          </button>
        </div>
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
