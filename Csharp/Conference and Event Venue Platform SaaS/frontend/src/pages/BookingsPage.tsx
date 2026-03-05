import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Layout } from '../components/Layout'
import { getBookings, confirmBooking, cancelBooking } from '../api/bookingsApi'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function BookingsPage() {
  const companySlug = useCompanySlug()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [cancelReason, setCancelReason] = useState<string>('')
  const [bookingToCancel, setBookingToCancel] = useState<string | null>(null)

  // Query for bookings list
  const { data: bookings, isLoading, isError } = useQuery({
    queryKey: ['bookings', companySlug],
    queryFn: () => getBookings(companySlug),
  })

  // Confirm booking mutation
  const confirmMutation = useMutation({
    mutationFn: (id: string) => confirmBooking(companySlug, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings', companySlug] })
      setError(null)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to confirm booking')
    },
  })

  // Cancel booking mutation
  const cancelMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      cancelBooking(companySlug, id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings', companySlug] })
      setBookingToCancel(null)
      setCancelReason('')
      setError(null)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to cancel booking')
    },
  })

  // Show API error
  if (isError && !error) {
    setError('Failed to load bookings. Please try again.')
  }

  const handleConfirm = (id: string) => {
    if (window.confirm('Are you sure you want to confirm this booking?')) {
      confirmMutation.mutate(id)
    }
  }

  const handleCancelClick = (id: string) => {
    setBookingToCancel(id)
    setCancelReason('')
  }

  const handleCancelSubmit = () => {
    if (bookingToCancel) {
      cancelMutation.mutate({ id: bookingToCancel, reason: cancelReason || undefined })
    }
  }

  const handleCancelDialogClose = () => {
    setBookingToCancel(null)
    setCancelReason('')
  }

  const formatDateTime = (utcString: string) => {
    const date = new Date(utcString)
    return date.toLocaleString()
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount)
  }

  const getStatusBadgeStyle = (status: string, isCancelled: boolean) => {
    if (isCancelled) {
      return { ...styles.statusBadge, ...styles.statusCancelled }
    }
    switch (status) {
      case 'Confirmed':
        return { ...styles.statusBadge, ...styles.statusConfirmed }
      case 'Pending':
      default:
        return { ...styles.statusBadge, ...styles.statusPending }
    }
  }

  const getStatusLabel = (status: string, isCancelled: boolean) => {
    if (isCancelled) return 'Cancelled'
    return status
  }

  return (
    <Layout>
      <div style={styles.container}>
        <div style={styles.header}>
          <h1 style={styles.title}>Bookings</h1>
          <button
            onClick={() => navigate(`/${companySlug}/bookings/new`)}
            style={styles.newButton}
          >
            + New Booking
          </button>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        {isLoading ? (
          <div style={styles.loading}>Loading bookings...</div>
        ) : (
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Title</th>
                  <th style={styles.th}>Client ID</th>
                  <th style={styles.th}>Start</th>
                  <th style={styles.th}>End</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th}>Total</th>
                  <th style={styles.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {bookings?.length === 0 ? (
                  <tr>
                    <td colSpan={7} style={styles.emptyCell}>
                      No bookings yet.
                    </td>
                  </tr>
                ) : (
                  bookings?.map((booking) => (
                    <tr key={booking.id} style={styles.tr}>
                      <td style={styles.td}>{booking.title}</td>
                      <td style={styles.td}>{booking.clientId.substring(0, 8)}...</td>
                      <td style={styles.td}>{formatDateTime(booking.startUtc)}</td>
                      <td style={styles.td}>{formatDateTime(booking.endUtc)}</td>
                      <td style={styles.td}>
                        <span style={getStatusBadgeStyle(booking.status, booking.isCancelled)}>
                          {getStatusLabel(booking.status, booking.isCancelled)}
                        </span>
                      </td>
                      <td style={styles.td}>{formatCurrency(booking.totalAmount)}</td>
                      <td style={styles.td}>
                        <div style={styles.actions}>
                          <button
                            onClick={() => navigate(`/${companySlug}/bookings/${booking.id}`)}
                            style={styles.actionButton}
                          >
                            View
                          </button>
                          {!booking.isCancelled && booking.status === 'Pending' && (
                            <>
                              <button
                                onClick={() => handleConfirm(booking.id)}
                                style={{ ...styles.actionButton, ...styles.confirmButton }}
                                disabled={confirmMutation.isPending}
                              >
                                Confirm
                              </button>
                              <button
                                onClick={() => handleCancelClick(booking.id)}
                                style={{ ...styles.actionButton, ...styles.cancelButton }}
                                disabled={cancelMutation.isPending}
                              >
                                Cancel
                              </button>
                            </>
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

        {/* Cancel Dialog */}
        {bookingToCancel && (
          <div style={styles.modalOverlay}>
            <div style={styles.modal}>
              <h3 style={styles.modalTitle}>Cancel Booking</h3>
              <p style={styles.modalText}>Please provide a reason for cancellation (optional):</p>
              <textarea
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder="Enter reason..."
                style={styles.textarea}
                rows={3}
              />
              <div style={styles.modalActions}>
                <button onClick={handleCancelDialogClose} style={styles.modalButton}>
                  Back
                </button>
                <button
                  onClick={handleCancelSubmit}
                  style={{ ...styles.modalButton, ...styles.modalConfirmButton }}
                  disabled={cancelMutation.isPending}
                >
                  {cancelMutation.isPending ? 'Cancelling...' : 'Cancel Booking'}
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
    maxWidth: '1200px',
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
    backgroundColor: '#007bff',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    fontSize: '14px',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
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
  statusBadge: {
    display: 'inline-block',
    padding: '4px 8px',
    borderRadius: '4px',
    fontSize: '12px',
    fontWeight: '600',
  },
  statusPending: {
    backgroundColor: '#fff3cd',
    color: '#856404',
  },
  statusConfirmed: {
    backgroundColor: '#d4edda',
    color: '#155724',
  },
  statusCancelled: {
    backgroundColor: '#f8d7da',
    color: '#721c24',
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
  confirmButton: {
    backgroundColor: '#28a745',
    color: 'white',
    borderColor: '#28a745',
  },
  cancelButton: {
    backgroundColor: '#dc3545',
    color: 'white',
    borderColor: '#dc3545',
  },
  modalOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0,0,0,0.5)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
  },
  modal: {
    backgroundColor: 'white',
    padding: '24px',
    borderRadius: '8px',
    width: '100%',
    maxWidth: '400px',
    boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
  },
  modalTitle: {
    margin: '0 0 16px 0',
    fontSize: '18px',
    fontWeight: 'bold',
  },
  modalText: {
    margin: '0 0 12px 0',
    fontSize: '14px',
    color: '#666',
  },
  textarea: {
    width: '100%',
    padding: '8px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    fontSize: '14px',
    fontFamily: 'inherit',
    resize: 'vertical',
    marginBottom: '16px',
  },
  modalActions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '8px',
  },
  modalButton: {
    padding: '8px 16px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    backgroundColor: 'white',
    cursor: 'pointer',
  },
  modalConfirmButton: {
    backgroundColor: '#dc3545',
    color: 'white',
    borderColor: '#dc3545',
  },
}
