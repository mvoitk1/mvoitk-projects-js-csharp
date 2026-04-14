import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getBookingDetails,
  confirmBooking,
  cancelBooking,
  createInvoiceFromBooking,
} from '../api/bookingsApi'
import { useCompanySlug } from '../hooks/useCompanySlug'

export function BookingDetailsPage() {
  const companySlug = useCompanySlug()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [cancelReason, setCancelReason] = useState<string>('')
  const [showCancelDialog, setShowCancelDialog] = useState(false)

  // Query for booking details
  const { data: booking, isLoading, isError } = useQuery({
    queryKey: ['booking', companySlug, id],
    queryFn: () => getBookingDetails(companySlug, id!),
    enabled: !!id,
  })

  // Confirm booking mutation
  const confirmMutation = useMutation({
    mutationFn: () => confirmBooking(companySlug, id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['booking', companySlug, id] })
      queryClient.invalidateQueries({ queryKey: ['bookings', companySlug] })
      setError(null)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to confirm booking')
    },
  })

  // Cancel booking mutation
  const cancelMutation = useMutation({
    mutationFn: (reason?: string) => cancelBooking(companySlug, id!, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['booking', companySlug, id] })
      queryClient.invalidateQueries({ queryKey: ['bookings', companySlug] })
      setShowCancelDialog(false)
      setCancelReason('')
      setError(null)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to cancel booking')
    },
  })

  // Create invoice from booking mutation
  const createInvoiceMutation = useMutation({
    mutationFn: () => createInvoiceFromBooking(companySlug, id!),
    onSuccess: (invoiceId) => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      navigate(`/${companySlug}/invoices/${invoiceId}`)
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to create invoice')
    },
  })

  // Show API error
  if (isError && !error) {
    setError('Failed to load booking details. Please try again.')
  }

  const handleConfirm = () => {
    if (window.confirm('Are you sure you want to confirm this booking?')) {
      confirmMutation.mutate()
    }
  }

  const handleCancelClick = () => {
    setShowCancelDialog(true)
    setCancelReason('')
  }

  const handleCancelSubmit = () => {
    cancelMutation.mutate(cancelReason || undefined)
  }

  const handleCancelDialogClose = () => {
    setShowCancelDialog(false)
    setCancelReason('')
  }

  const handleCreateInvoice = () => {
    if (window.confirm('Are you sure you want to create an invoice from this booking?')) {
      createInvoiceMutation.mutate()
    }
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

  if (isLoading) {
    return (
      
        <div style={styles.container}>
          <div style={styles.loading}>Loading booking details...</div>
        </div>
      
    )
  }

  if (!booking) {
    return (
      
        <div style={styles.container}>
          <div style={styles.errorBanner}>Booking not found</div>
          <button
            onClick={() => navigate(`/${companySlug}/bookings`)}
            style={styles.backButton}
          >
            Back to Bookings
          </button>
        </div>
      
    )
  }

  return (
    
      <div style={styles.container}>
        <div style={styles.header}>
          <div>
            <button
              onClick={() => navigate(`/${companySlug}/bookings`)}
              style={styles.backLink}
            >
              ← Back to Bookings
            </button>
            <h1 style={styles.title}>{booking.title}</h1>
          </div>
          <div style={styles.headerActions}>
            {!booking.isCancelled && booking.status === 'Pending' && (
              <>
                <button
                  onClick={handleConfirm}
                  style={{ ...styles.actionButton, ...styles.confirmButton }}
                  disabled={confirmMutation.isPending}
                >
                  {confirmMutation.isPending ? 'Confirming...' : 'Confirm Booking'}
                </button>
                <button
                  onClick={handleCancelClick}
                  style={{ ...styles.actionButton, ...styles.cancelButton }}
                  disabled={cancelMutation.isPending}
                >
                  {cancelMutation.isPending ? 'Cancelling...' : 'Cancel Booking'}
                </button>
              </>
            )}
            {!booking.isCancelled && booking.status === 'Confirmed' && (
              <button
                onClick={handleCreateInvoice}
                style={{ ...styles.actionButton, ...styles.invoiceButton }}
                disabled={createInvoiceMutation.isPending}
              >
                {createInvoiceMutation.isPending ? 'Creating Invoice...' : 'Create Invoice'}
              </button>
            )}
          </div>
        </div>

        {error && (
          <div style={styles.errorBanner}>
            {error}
            <button onClick={() => setError(null)} style={styles.closeError}>×</button>
          </div>
        )}

        <div style={styles.detailsCard}>
          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Status</span>
            <span style={getStatusBadgeStyle(booking.status, booking.isCancelled)}>
              {getStatusLabel(booking.status, booking.isCancelled)}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Client</span>
            <span style={styles.detailValue}>{booking.clientName}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Start Time</span>
            <span style={styles.detailValue}>{formatDateTime(booking.startUtc)}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>End Time</span>
            <span style={styles.detailValue}>{formatDateTime(booking.endUtc)}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Attendee Count</span>
            <span style={styles.detailValue}>{booking.attendeeCount}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Spaces</span>
            <span style={styles.detailValue}>
              {booking.spaces.length > 0
                ? booking.spaces.map((s) => s.name).join(', ')
                : 'No spaces assigned'}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Total Amount</span>
            <span style={{ ...styles.detailValue, ...styles.amountValue }}>
              {formatCurrency(booking.totalAmount)}
            </span>
          </div>

          {booking.isCancelled && (
            <div style={styles.cancellationSection}>
              <h3 style={styles.sectionTitle}>Cancellation Details</h3>
              <div style={styles.detailRow}>
                <span style={styles.detailLabel}>Cancelled At</span>
                <span style={styles.detailValue}>
                  {booking.cancelledUtc ? formatDateTime(booking.cancelledUtc) : 'N/A'}
                </span>
              </div>
              {booking.cancelReason && (
                <div style={styles.detailRow}>
                  <span style={styles.detailLabel}>Reason</span>
                  <span style={styles.detailValue}>{booking.cancelReason}</span>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Cancel Dialog */}
        {showCancelDialog && (
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
  actionButton: {
    padding: '10px 20px',
    fontSize: '14px',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
  },
  confirmButton: {
    backgroundColor: '#28a745',
    color: 'white',
  },
  cancelButton: {
    backgroundColor: '#dc3545',
    color: 'white',
  },
  invoiceButton: {
    backgroundColor: '#007bff',
    color: 'white',
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
  amountValue: {
    fontWeight: 'bold',
    color: '#28a745',
  },
  statusBadge: {
    display: 'inline-block',
    padding: '4px 12px',
    borderRadius: '4px',
    fontSize: '14px',
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
  cancellationSection: {
    marginTop: '24px',
    paddingTop: '20px',
    borderTop: '2px solid #e0e0e0',
  },
  sectionTitle: {
    fontSize: '16px',
    fontWeight: 'bold',
    color: '#333',
    margin: '0 0 16px 0',
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
