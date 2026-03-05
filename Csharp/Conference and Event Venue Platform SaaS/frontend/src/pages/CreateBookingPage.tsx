import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { createBookingWithSpaces } from '../api/bookingsApi'
import { getClients } from '../api/clientsApi'
import { getSpaces } from '../api/spacesApi'
import { ApiError, BookingConflictResponse } from '../types/apiTypes'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function CreateBookingPage() {
  const companySlug = useCompanySlug()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [title, setTitle] = useState('')
  const [clientId, setClientId] = useState('')
  const [startUtc, setStartUtc] = useState('')
  const [endUtc, setEndUtc] = useState('')
  const [attendeeCount, setAttendeeCount] = useState('')
  const [selectedSpaceIds, setSelectedSpaceIds] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const [conflictInfo, setConflictInfo] = useState<BookingConflictResponse | null>(null)

  // Fetch clients and spaces
  const { data: clients, isLoading: isLoadingClients } = useQuery({
    queryKey: ['clients', companySlug],
    queryFn: () => getClients(companySlug),
  })

  const { data: spaces, isLoading: isLoadingSpaces } = useQuery({
    queryKey: ['spaces', companySlug],
    queryFn: () => getSpaces(companySlug),
  })

  // Create booking mutation
  const createMutation = useMutation({
    mutationFn: () =>
      createBookingWithSpaces(companySlug, {
        clientId,
        title,
        startUtc: new Date(startUtc).toISOString(),
        endUtc: new Date(endUtc).toISOString(),
        attendeeCount: parseInt(attendeeCount, 10) || 0,
        spaceIds: selectedSpaceIds,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings', companySlug] })
      navigate(`/${companySlug}/bookings`)
    },
    onError: (err: Error) => {
      const apiError = err as ApiError
      if (apiError.code === 'booking_conflict') {
        setError('Selected space is already booked in that time range.')
        // Try to extract conflict info from details if available
        if (apiError.details && 'conflictingBookingIds' in apiError.details) {
          setConflictInfo({
            error: 'Booking conflicts with existing bookings.',
            conflictingBookingIds: apiError.details.conflictingBookingIds as string[],
            conflictingSpaceIds: apiError.details.conflictingSpaceIds as string[],
          })
        }
      } else if (err.message?.includes('Confirmed booking cannot be modified')) {
        setError('Confirmed booking cannot be modified.')
      } else if (err.message?.includes('Cancelled booking cannot be modified')) {
        setError('Cancelled booking cannot be modified.')
      } else {
        setError(err.message || 'Failed to create booking')
      }
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setConflictInfo(null)

    // Validation
    if (!title.trim()) {
      setError('Title is required')
      return
    }
    if (!clientId) {
      setError('Please select a client')
      return
    }
    if (!startUtc) {
      setError('Start time is required')
      return
    }
    if (!endUtc) {
      setError('End time is required')
      return
    }
    if (new Date(startUtc) >= new Date(endUtc)) {
      setError('Start time must be before end time')
      return
    }
    if (selectedSpaceIds.length === 0) {
      setError('Please select at least one space')
      return
    }

    createMutation.mutate()
  }

  const handleSpaceToggle = (spaceId: string) => {
    setSelectedSpaceIds((prev) =>
      prev.includes(spaceId)
        ? prev.filter((id) => id !== spaceId)
        : [...prev, spaceId]
    )
  }

  const isLoading = isLoadingClients || isLoadingSpaces

  return (
    
      <div style={styles.container}>
        <div style={styles.header}>
          <h1 style={styles.title}>New Booking</h1>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            <div>
              <strong>Error:</strong> {error}
              {conflictInfo && (
                <div style={styles.conflictInfo}>
                  <p>Conflicting booking IDs: {conflictInfo.conflictingBookingIds.join(', ')}</p>
                  {conflictInfo.conflictingSpaceIds.length > 0 && (
                    <p>Conflicting space IDs: {conflictInfo.conflictingSpaceIds.join(', ')}</p>
                  )}
                </div>
              )}
            </div>
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        {isLoading ? (
          <div style={styles.loading}>Loading...</div>
        ) : (
          <form onSubmit={handleSubmit} style={styles.form}>
            <div style={styles.formGroup}>
              <label htmlFor="title" style={styles.label}>
                Title *
              </label>
              <input
                id="title"
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Enter booking title"
                style={styles.input}
                maxLength={200}
              />
            </div>

            <div style={styles.formGroup}>
              <label htmlFor="client" style={styles.label}>
                Client *
              </label>
              <select
                id="client"
                value={clientId}
                onChange={(e) => setClientId(e.target.value)}
                style={styles.select}
              >
                <option value="">Select a client</option>
                {clients?.map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.name}
                  </option>
                ))}
              </select>
            </div>

            <div style={styles.formRow}>
              <div style={{ ...styles.formGroup, flex: 1 }}>
                <label htmlFor="startUtc" style={styles.label}>
                  Start Time *
                </label>
                <input
                  id="startUtc"
                  type="datetime-local"
                  value={startUtc}
                  onChange={(e) => setStartUtc(e.target.value)}
                  style={styles.input}
                />
              </div>

              <div style={{ ...styles.formGroup, flex: 1 }}>
                <label htmlFor="endUtc" style={styles.label}>
                  End Time *
                </label>
                <input
                  id="endUtc"
                  type="datetime-local"
                  value={endUtc}
                  onChange={(e) => setEndUtc(e.target.value)}
                  style={styles.input}
                />
              </div>
            </div>

            <div style={styles.formGroup}>
              <label htmlFor="attendeeCount" style={styles.label}>
                Attendee Count
              </label>
              <input
                id="attendeeCount"
                type="number"
                min="0"
                value={attendeeCount}
                onChange={(e) => setAttendeeCount(e.target.value)}
                placeholder="0"
                style={styles.input}
              />
            </div>

            <div style={styles.formGroup}>
              <label style={styles.label}>Spaces *</label>
              <div style={styles.spacesContainer}>
                {spaces?.length === 0 ? (
                  <p style={styles.noSpaces}>No spaces available. Please create a space first.</p>
                ) : (
                  spaces?.map((space) => (
                    <label key={space.id} style={styles.spaceCheckbox}>
                      <input
                        type="checkbox"
                        checked={selectedSpaceIds.includes(space.id)}
                        onChange={() => handleSpaceToggle(space.id)}
                        style={styles.checkbox}
                      />
                      <span style={styles.spaceName}>{space.name}</span>
                      <span style={styles.spaceInfo}>
                        (Capacity: {space.capacity})
                      </span>
                    </label>
                  ))
                )}
              </div>
            </div>

            <div style={styles.formActions}>
              <button
                type="button"
                onClick={() => navigate(`/${companySlug}/bookings`)}
                style={styles.cancelButton}
              >
                Cancel
              </button>
              <button
                type="submit"
                style={styles.submitButton}
                disabled={createMutation.isPending}
              >
                {createMutation.isPending ? 'Creating...' : 'Create Booking'}
              </button>
            </div>
          </form>
        )}
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
    marginBottom: '24px',
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
    alignItems: 'flex-start',
  },
  conflictInfo: {
    marginTop: '8px',
    fontSize: '13px',
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
  form: {
    backgroundColor: 'white',
    padding: '24px',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
  },
  formGroup: {
    marginBottom: '20px',
  },
  formRow: {
    display: 'flex',
    gap: '16px',
    marginBottom: '20px',
  },
  label: {
    display: 'block',
    marginBottom: '8px',
    fontSize: '14px',
    fontWeight: '600',
    color: '#333',
  },
  input: {
    width: '100%',
    padding: '10px 12px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    boxSizing: 'border-box',
  },
  select: {
    width: '100%',
    padding: '10px 12px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    backgroundColor: 'white',
    cursor: 'pointer',
  },
  spacesContainer: {
    border: '1px solid #ddd',
    borderRadius: '4px',
    padding: '12px',
    maxHeight: '200px',
    overflowY: 'auto',
  },
  spaceCheckbox: {
    display: 'flex',
    alignItems: 'center',
    padding: '8px 0',
    cursor: 'pointer',
  },
  checkbox: {
    marginRight: '8px',
  },
  spaceName: {
    fontWeight: '500',
    marginRight: '8px',
  },
  spaceInfo: {
    color: '#666',
    fontSize: '13px',
  },
  noSpaces: {
    color: '#666',
    fontStyle: 'italic',
  },
  formActions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '12px',
    marginTop: '24px',
    paddingTop: '20px',
    borderTop: '1px solid #e0e0e0',
  },
  cancelButton: {
    padding: '10px 20px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    backgroundColor: 'white',
    cursor: 'pointer',
  },
  submitButton: {
    padding: '10px 20px',
    fontSize: '14px',
    border: 'none',
    borderRadius: '4px',
    backgroundColor: '#007bff',
    color: 'white',
    cursor: 'pointer',
  },
}
