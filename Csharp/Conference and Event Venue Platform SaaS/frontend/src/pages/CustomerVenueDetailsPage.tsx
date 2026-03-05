import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { getPublicCompanies } from '../api/publicCompaniesApi'
import { createBookingRequest } from '../api/bookingRequestsApi'
import { getPublicSpaces } from '../api/publicSpacesApi'
import { normalizeApiError } from '../api/apiClient'
import type { PublicCompanyDto } from '../types/apiTypes'

export function CustomerVenueDetailsPage() {
  const navigate = useNavigate()
  const { venueSlug } = useParams<{ venueSlug: string }>()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const [contactName, setContactName] = useState(localStorage.getItem('venue_platform_contact_name') ?? '')
  const [contactEmail, setContactEmail] = useState(
    localStorage.getItem('venue_platform_user_email') ??
      localStorage.getItem('email') ??
      ''
  )
  const [contactPhone, setContactPhone] = useState('')
  const [notes, setNotes] = useState('')
  const [startUtc, setStartUtc] = useState('')
  const [endUtc, setEndUtc] = useState('')
  const [selectedSpaceIds, setSelectedSpaceIds] = useState<string[]>([])

  const { data: response, isLoading, error } = useQuery({
    queryKey: ['publicCompanies'],
    queryFn: getPublicCompanies,
  })

  const {
    data: spaces,
    isLoading: isLoadingSpaces,
    error: spacesError,
  } = useQuery({
    queryKey: ['publicSpaces', venueSlug],
    queryFn: () => getPublicSpaces(venueSlug!),
    enabled: isModalOpen && !!venueSlug,
  })

  // Find the company from the cached list
  const company = response?.companies.find(
    (c: PublicCompanyDto) => c.companySlug === venueSlug
  )

  const sortedSpaces = useMemo(
    () => (spaces ?? []).slice().sort((a, b) => a.name.localeCompare(b.name)),
    [spaces]
  )

  const bookingMutation = useMutation({
    mutationFn: () => {
      if (!venueSlug) {
        throw new Error('Venue slug is missing')
      }

      return createBookingRequest(venueSlug, {
        contactName: contactName.trim(),
        contactEmail: contactEmail.trim(),
        contactPhone: contactPhone.trim() || undefined,
        notes: notes.trim() || undefined,
        startUtc: new Date(startUtc).toISOString(),
        endUtc: new Date(endUtc).toISOString(),
        spaceIds: selectedSpaceIds,
      })
    },
    onSuccess: () => {
      if (contactName.trim()) {
        localStorage.setItem('venue_platform_contact_name', contactName.trim())
      }

      setIsModalOpen(false)
      setFormError(null)
      setSuccessMessage(`Booking request sent to ${company?.companyName ?? 'venue'}`)
    },
    onError: (err: unknown) => {
      const normalized = normalizeApiError(err)
      setFormError(normalized.message || 'Failed to send booking request. Please try again.')
    },
  })

  const toggleSpace = (spaceId: string) => {
    setSelectedSpaceIds((prev) =>
      prev.includes(spaceId)
        ? prev.filter((id) => id !== spaceId)
        : [...prev, spaceId]
    )
  }

  const openModal = () => {
    setFormError(null)
    setSuccessMessage(null)
    setIsModalOpen(true)
  }

  const closeModal = () => {
    if (bookingMutation.isPending) {
      return
    }
    setIsModalOpen(false)
    setFormError(null)
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)

    if (!contactName.trim()) {
      setFormError('Contact name is required.')
      return
    }

    if (!contactEmail.trim()) {
      setFormError('Contact email is required.')
      return
    }

    if (!startUtc || !endUtc) {
      setFormError('Start and end time are required.')
      return
    }

    if (new Date(endUtc) <= new Date(startUtc)) {
      setFormError('End time must be after start time.')
      return
    }

    if (selectedSpaceIds.length === 0) {
      setFormError('Select at least one space.')
      return
    }

    bookingMutation.mutate()
  }

  if (isLoading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading venue details...</div>
      </div>
    )
  }

  if (error || !company) {
    return (
      <div style={styles.container}>
        <div style={styles.error}>
          <h2 style={styles.errorTitle}>Venue Not Found</h2>
          <p style={styles.errorText}>
            The venue you are looking for does not exist or has been removed.
          </p>
          <button
            onClick={() => navigate('/customer/venues')}
            style={styles.backButton}
          >
            ← Back to Venues
          </button>
        </div>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <button
        onClick={() => navigate('/customer/venues')}
        style={styles.backLink}
      >
        ← Back to Venues
      </button>

      <div style={styles.header}>
        <h1 style={styles.title}>{company.companyName}</h1>
        <span style={styles.planBadge}>{company.plan}</span>
      </div>

      {successMessage && (
        <div style={styles.successBanner}>
          {successMessage}
        </div>
      )}

      <div style={styles.card}>
        <div style={styles.section}>
          <h2 style={styles.sectionTitle}>Venue Information</h2>
          <div style={styles.infoGrid}>
            <div style={styles.infoItem}>
              <span style={styles.infoLabel}>Name</span>
              <span style={styles.infoValue}>{company.companyName}</span>
            </div>
            <div style={styles.infoItem}>
              <span style={styles.infoLabel}>Slug</span>
              <span style={styles.infoValue}>@{company.companySlug}</span>
            </div>
            <div style={styles.infoItem}>
              <span style={styles.infoLabel}>Plan</span>
              <span style={styles.infoValue}>{company.plan}</span>
            </div>
          </div>
        </div>

        <div style={styles.divider} />

        <div style={styles.section}>
          <h2 style={styles.sectionTitle}>Booking</h2>
          <button
            style={styles.bookingButton}
            onClick={openModal}
          >
            Request booking
          </button>
        </div>
      </div>

      {isModalOpen && (
        <div style={styles.modalOverlay}>
          <div style={styles.modal}>
            <h3 style={styles.modalTitle}>Request booking</h3>

            {formError && (
              <div style={styles.errorBanner}>
                {formError}
              </div>
            )}

            <form onSubmit={handleSubmit} style={styles.form}>
              <label style={styles.fieldLabel} htmlFor="contactName">Contact name *</label>
              <input
                id="contactName"
                type="text"
                value={contactName}
                onChange={(e) => setContactName(e.target.value)}
                style={styles.input}
                maxLength={200}
              />

              <label style={styles.fieldLabel} htmlFor="contactEmail">Contact email *</label>
              <input
                id="contactEmail"
                type="email"
                value={contactEmail}
                onChange={(e) => setContactEmail(e.target.value)}
                style={styles.input}
                maxLength={256}
              />

              <label style={styles.fieldLabel} htmlFor="contactPhone">Phone</label>
              <input
                id="contactPhone"
                type="text"
                value={contactPhone}
                onChange={(e) => setContactPhone(e.target.value)}
                style={styles.input}
                maxLength={64}
              />

              <div style={styles.dateRow}>
                <div style={styles.dateCol}>
                  <label style={styles.fieldLabel} htmlFor="startUtc">Start *</label>
                  <input
                    id="startUtc"
                    type="datetime-local"
                    value={startUtc}
                    onChange={(e) => setStartUtc(e.target.value)}
                    style={styles.input}
                  />
                </div>
                <div style={styles.dateCol}>
                  <label style={styles.fieldLabel} htmlFor="endUtc">End *</label>
                  <input
                    id="endUtc"
                    type="datetime-local"
                    value={endUtc}
                    onChange={(e) => setEndUtc(e.target.value)}
                    style={styles.input}
                  />
                </div>
              </div>

              <label style={styles.fieldLabel}>Spaces *</label>
              <div style={styles.spacesBox}>
                {isLoadingSpaces && <div style={styles.inlineText}>Loading spaces...</div>}
                {!isLoadingSpaces && spacesError && (
                  <div style={styles.inlineText}>Could not load spaces. Please retry.</div>
                )}
                {!isLoadingSpaces && !spacesError && sortedSpaces.length === 0 && (
                  <div style={styles.inlineText}>No public spaces available.</div>
                )}
                {!isLoadingSpaces && !spacesError && sortedSpaces.map((space) => (
                  <label key={space.id} style={styles.spaceOption}>
                    <input
                      type="checkbox"
                      checked={selectedSpaceIds.includes(space.id)}
                      onChange={() => toggleSpace(space.id)}
                    />
                    <span>{space.name} (capacity {space.capacity})</span>
                  </label>
                ))}
              </div>

              <label style={styles.fieldLabel} htmlFor="notes">Notes</label>
              <textarea
                id="notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                style={styles.textarea}
                rows={3}
                maxLength={1000}
              />

              <div style={styles.modalActions}>
                <button type="button" onClick={closeModal} style={styles.cancelButton}>
                  Cancel
                </button>
                <button type="submit" style={styles.submitButton} disabled={bookingMutation.isPending}>
                  {bookingMutation.isPending ? 'Sending...' : 'Send request'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '800px',
    margin: '0 auto',
  },
  backLink: {
    padding: '8px 0',
    fontSize: '14px',
    color: '#2563eb',
    backgroundColor: 'transparent',
    border: 'none',
    cursor: 'pointer',
    marginBottom: '16px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: '16px',
    marginBottom: '24px',
  },
  title: {
    fontSize: '32px',
    fontWeight: 'bold',
    color: '#333',
    margin: 0,
  },
  planBadge: {
    fontSize: '14px',
    fontWeight: '500',
    padding: '6px 12px',
    backgroundColor: '#dbeafe',
    color: '#1e40af',
    borderRadius: '6px',
  },
  successBanner: {
    padding: '12px 16px',
    marginBottom: '16px',
    borderRadius: '8px',
    backgroundColor: '#ecfdf3',
    color: '#065f46',
    border: '1px solid #a7f3d0',
    fontSize: '14px',
    fontWeight: '500',
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  error: {
    padding: '40px',
    textAlign: 'center',
    backgroundColor: '#f9fafb',
    borderRadius: '12px',
    border: '2px dashed #e5e7eb',
  },
  errorTitle: {
    fontSize: '24px',
    fontWeight: 'bold',
    color: '#333',
    margin: '0 0 8px 0',
  },
  errorText: {
    fontSize: '16px',
    color: '#666',
    margin: '0 0 24px 0',
  },
  backButton: {
    padding: '10px 20px',
    fontSize: '14px',
    backgroundColor: '#2563eb',
    color: 'white',
    border: 'none',
    borderRadius: '6px',
    cursor: 'pointer',
  },
  card: {
    backgroundColor: 'white',
    borderRadius: '12px',
    padding: '24px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    border: '1px solid #e5e7eb',
  },
  section: {
    marginBottom: '24px',
  },
  sectionTitle: {
    fontSize: '18px',
    fontWeight: '600',
    color: '#333',
    margin: '0 0 16px 0',
  },
  infoGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '16px',
  },
  infoItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  infoLabel: {
    fontSize: '12px',
    fontWeight: '500',
    color: '#6b7280',
    textTransform: 'uppercase',
  },
  infoValue: {
    fontSize: '16px',
    color: '#333',
  },
  divider: {
    height: '1px',
    backgroundColor: '#e5e7eb',
    margin: '24px 0',
  },
  comingSoon: {
    fontSize: '14px',
    color: '#6b7280',
    margin: '0 0 16px 0',
  },
  bookingButton: {
    padding: '12px 24px',
    fontSize: '14px',
    fontWeight: '500',
    backgroundColor: '#2563eb',
    color: '#fff',
    border: '1px solid #2563eb',
    borderRadius: '6px',
    cursor: 'pointer',
  },
  modalOverlay: {
    position: 'fixed',
    inset: 0,
    backgroundColor: 'rgba(15, 23, 42, 0.5)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
    padding: '16px',
  },
  modal: {
    width: '100%',
    maxWidth: '640px',
    maxHeight: '90vh',
    overflowY: 'auto',
    backgroundColor: '#fff',
    borderRadius: '12px',
    padding: '20px',
  },
  modalTitle: {
    margin: '0 0 16px 0',
    fontSize: '20px',
    color: '#111827',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '10px',
  },
  fieldLabel: {
    fontSize: '13px',
    fontWeight: '600',
    color: '#374151',
  },
  input: {
    border: '1px solid #d1d5db',
    borderRadius: '6px',
    padding: '10px 12px',
    fontSize: '14px',
  },
  textarea: {
    border: '1px solid #d1d5db',
    borderRadius: '6px',
    padding: '10px 12px',
    fontSize: '14px',
    resize: 'vertical',
  },
  dateRow: {
    display: 'flex',
    gap: '10px',
  },
  dateCol: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: '10px',
  },
  spacesBox: {
    border: '1px solid #e5e7eb',
    borderRadius: '6px',
    padding: '10px',
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    maxHeight: '180px',
    overflowY: 'auto',
  },
  spaceOption: {
    display: 'flex',
    gap: '8px',
    alignItems: 'center',
    fontSize: '14px',
    color: '#374151',
  },
  inlineText: {
    fontSize: '13px',
    color: '#6b7280',
  },
  errorBanner: {
    marginBottom: '12px',
    border: '1px solid #fecaca',
    backgroundColor: '#fef2f2',
    color: '#b91c1c',
    borderRadius: '6px',
    padding: '10px 12px',
    fontSize: '14px',
  },
  modalActions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '8px',
    marginTop: '10px',
  },
  cancelButton: {
    padding: '10px 14px',
    borderRadius: '6px',
    border: '1px solid #d1d5db',
    backgroundColor: '#fff',
    color: '#374151',
    cursor: 'pointer',
    fontSize: '14px',
  },
  submitButton: {
    padding: '10px 14px',
    borderRadius: '6px',
    border: '1px solid #2563eb',
    backgroundColor: '#2563eb',
    color: '#fff',
    cursor: 'pointer',
    fontSize: '14px',
    fontWeight: '600',
  },
}
