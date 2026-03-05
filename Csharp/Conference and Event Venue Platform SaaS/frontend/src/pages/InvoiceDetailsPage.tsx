import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Layout } from '../components/Layout'
import { getInvoice, issueInvoice, markInvoiceSent, voidInvoice, markInvoicePaid, getInvoicePayments, recordInvoicePayment } from '../api/invoicesApi'
import { useCompanySlug } from '../hooks/useCompanySlug'
import type { InvoiceStatus } from '../types/apiTypes'

export function InvoiceDetailsPage() {
  const companySlug = useCompanySlug()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // Confirmation dialog state
  const [confirmDialog, setConfirmDialog] = useState<{ type: 'void' | 'mark-paid' | null; title: string; message: string } | null>(null)

  // Record payment form state
  const [showRecordPaymentForm, setShowRecordPaymentForm] = useState(false)
  const [paymentFormData, setPaymentFormData] = useState({
    amount: '',
    paidUtc: new Date().toISOString().split('T')[0],
    method: '',
    reference: '',
  })

  // Query for invoice details
  const { data: invoice, isLoading, isError, error } = useQuery({
    queryKey: ['invoice', companySlug, id],
    queryFn: () => getInvoice(companySlug, id!),
    enabled: !!id,
  })

  // Mutations for invoice actions
  const issueMutation = useMutation({
    mutationFn: () => issueInvoice(companySlug, id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', companySlug] })
      queryClient.invalidateQueries({ queryKey: ['invoice', companySlug, id] })
    },
  })

  const markSentMutation = useMutation({
    mutationFn: () => markInvoiceSent(companySlug, id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', companySlug] })
      queryClient.invalidateQueries({ queryKey: ['invoice', companySlug, id] })
    },
  })

  const voidMutation = useMutation({
    mutationFn: () => voidInvoice(companySlug, id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', companySlug] })
      queryClient.invalidateQueries({ queryKey: ['invoice', companySlug, id] })
      setConfirmDialog(null)
    },
  })

  const markPaidMutation = useMutation({
    mutationFn: () => markInvoicePaid(companySlug, id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', companySlug] })
      queryClient.invalidateQueries({ queryKey: ['invoice', companySlug, id] })
      setConfirmDialog(null)
    },
  })

  // Query for payments
  const { data: payments, isLoading: isLoadingPayments } = useQuery({
    queryKey: ['invoicePayments', companySlug, id],
    queryFn: () => getInvoicePayments(companySlug, id!),
    enabled: !!id,
  })

  // Mutation for recording payment
  const recordPaymentMutation = useMutation({
    mutationFn: () => {
      const payload = {
        amount: parseFloat(paymentFormData.amount),
        paidUtc: new Date(paymentFormData.paidUtc).toISOString(),
        method: paymentFormData.method,
        reference: paymentFormData.reference || undefined,
      }
      return recordInvoicePayment(companySlug, id!, payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoicePayments', companySlug, id] })
      queryClient.invalidateQueries({ queryKey: ['invoice', companySlug, id] })
      queryClient.invalidateQueries({ queryKey: ['invoices', companySlug] })
      setPaymentFormData({
        amount: '',
        paidUtc: new Date().toISOString().split('T')[0],
        method: '',
        reference: '',
      })
      setShowRecordPaymentForm(false)
    },
  })

  const isAnyMutationPending = issueMutation.isPending || markSentMutation.isPending || voidMutation.isPending || markPaidMutation.isPending || recordPaymentMutation.isPending

  // Visibility rules for action buttons
  const canShowIssue = invoice?.status === 'Draft'
  const canShowVoid = invoice?.status === 'Draft' || invoice?.status === 'Issued' || invoice?.status === 'Sent'
  const canShowMarkSent = invoice?.status === 'Issued'
  const canShowMarkPaid = invoice?.status === 'Issued' || invoice?.status === 'Sent'
  const canShowRecordPayment = invoice?.status !== 'Void'
  const hasAnyActions = canShowIssue || canShowVoid || canShowMarkSent || canShowMarkPaid || canShowRecordPayment

  const handleIssue = () => {
    issueMutation.mutate()
  }

  const handleMarkSent = () => {
    markSentMutation.mutate()
  }

  const handleVoidClick = () => {
    setConfirmDialog({
      type: 'void',
      title: 'Void Invoice',
      message: 'Are you sure you want to void this invoice? This action cannot be undone.',
    })
  }

  const handleMarkPaidClick = () => {
    setConfirmDialog({
      type: 'mark-paid',
      title: 'Mark as Paid',
      message: 'Are you sure you want to mark this invoice as paid?',
    })
  }

  const handleConfirmAction = () => {
    if (confirmDialog?.type === 'void') {
      voidMutation.mutate()
    } else if (confirmDialog?.type === 'mark-paid') {
      markPaidMutation.mutate()
    }
  }

  const handleCancelConfirm = () => {
    setConfirmDialog(null)
  }

  const handleRecordPaymentClick = () => {
    setShowRecordPaymentForm(true)
  }

  const handleCancelRecordPayment = () => {
    setShowRecordPaymentForm(false)
    setPaymentFormData({
      amount: '',
      paidUtc: new Date().toISOString().split('T')[0],
      method: '',
      reference: '',
    })
  }

  const handlePaymentFormChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    setPaymentFormData(prev => ({ ...prev, [name]: value }))
  }

  const handleSubmitPayment = (e: React.FormEvent) => {
    e.preventDefault()
    if (!paymentFormData.amount || !paymentFormData.method) return
    recordPaymentMutation.mutate()
  }

  const formatDateTime = (utcString: string | undefined) => {
    if (!utcString) return 'N/A'
    const date = new Date(utcString)
    return date.toLocaleString()
  }

  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount)
  }

  const getStatusBadgeStyle = (status: InvoiceStatus) => {
    switch (status) {
      case 'Paid':
        return { ...styles.statusBadge, ...styles.statusPaid }
      case 'Issued':
      case 'Sent':
        return { ...styles.statusBadge, ...styles.statusIssued }
      case 'Void':
        return { ...styles.statusBadge, ...styles.statusVoid }
      case 'Draft':
      default:
        return { ...styles.statusBadge, ...styles.statusDraft }
    }
  }

  const getErrorMessage = () => {
    if (error instanceof Error) {
      return error.message
    }
    return 'Failed to load invoice details. Please try again.'
  }

  if (isLoading) {
    return (
      <Layout>
        <div style={styles.container}>
          <div style={styles.loading}>Loading invoice details...</div>
        </div>
      </Layout>
    )
  }

  if (isError || !invoice) {
    return (
      <Layout>
        <div style={styles.container}>
          <div style={styles.errorBanner}>{isError ? getErrorMessage() : 'Invoice not found'}</div>
          <button
            onClick={() => navigate(`/${companySlug}/invoices`)}
            style={styles.backButton}
          >
            Back to Invoices
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
              onClick={() => navigate(`/${companySlug}/invoices`)}
              style={styles.backLink}
            >
              ← Back to Invoices
            </button>
            <h1 style={styles.title}>Invoice {invoice.invoiceNumberText}</h1>
          </div>
          <div style={styles.headerActions}>
            {hasAnyActions && (
              <>
                {canShowIssue && (
                  <button
                    onClick={handleIssue}
                    disabled={isAnyMutationPending}
                    style={{ ...styles.actionButton, ...styles.primaryButton }}
                  >
                    {issueMutation.isPending ? 'Issuing...' : 'Issue'}
                  </button>
                )}
                {canShowMarkSent && (
                  <button
                    onClick={handleMarkSent}
                    disabled={isAnyMutationPending}
                    style={{ ...styles.actionButton, ...styles.secondaryButton }}
                  >
                    {markSentMutation.isPending ? 'Marking...' : 'Mark Sent'}
                  </button>
                )}
                {canShowMarkPaid && (
                  <button
                    onClick={handleMarkPaidClick}
                    disabled={isAnyMutationPending}
                    style={{ ...styles.actionButton, ...styles.successButton }}
                  >
                    {markPaidMutation.isPending ? 'Processing...' : 'Mark Paid'}
                  </button>
                )}
                {canShowRecordPayment && (
                  <button
                    onClick={handleRecordPaymentClick}
                    disabled={isAnyMutationPending}
                    style={{ ...styles.actionButton, ...styles.primaryButton }}
                  >
                    Record Payment
                  </button>
                )}
                {canShowVoid && (
                  <button
                    onClick={handleVoidClick}
                    disabled={isAnyMutationPending}
                    style={{ ...styles.actionButton, ...styles.dangerButton }}
                  >
                    {voidMutation.isPending ? 'Voiding...' : 'Void'}
                  </button>
                )}
              </>
            )}
            <span style={getStatusBadgeStyle(invoice.status)}>
              {invoice.status}
            </span>
          </div>
        </div>

        {/* Confirmation Dialog */}
        {confirmDialog && (
          <div style={styles.modalOverlay}>
            <div style={styles.modal}>
              <h3 style={styles.modalTitle}>{confirmDialog.title}</h3>
              <p style={styles.modalMessage}>{confirmDialog.message}</p>
              <div style={styles.modalActions}>
                <button
                  onClick={handleCancelConfirm}
                  style={{ ...styles.modalButton, ...styles.modalButtonSecondary }}
                >
                  Cancel
                </button>
                <button
                  onClick={handleConfirmAction}
                  disabled={isAnyMutationPending}
                  style={{
                    ...styles.modalButton,
                    ...(confirmDialog.type === 'void' ? styles.modalButtonDanger : styles.modalButtonSuccess),
                  }}
                >
                  {isAnyMutationPending ? 'Processing...' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Record Payment Form Modal */}
        {showRecordPaymentForm && (
          <div style={styles.modalOverlay}>
            <div style={{ ...styles.modal, maxWidth: '500px' }}>
              <h3 style={styles.modalTitle}>Record Payment</h3>
              <form onSubmit={handleSubmitPayment}>
                <div style={styles.formGroup}>
                  <label style={styles.formLabel}>Amount *</label>
                  <input
                    type="number"
                    name="amount"
                    value={paymentFormData.amount}
                    onChange={handlePaymentFormChange}
                    placeholder="0.00"
                    step="0.01"
                    min="0.01"
                    required
                    style={styles.formInput}
                  />
                </div>
                <div style={styles.formGroup}>
                  <label style={styles.formLabel}>Date *</label>
                  <input
                    type="date"
                    name="paidUtc"
                    value={paymentFormData.paidUtc}
                    onChange={handlePaymentFormChange}
                    required
                    style={styles.formInput}
                  />
                </div>
                <div style={styles.formGroup}>
                  <label style={styles.formLabel}>Method *</label>
                  <select
                    name="method"
                    value={paymentFormData.method}
                    onChange={handlePaymentFormChange}
                    required
                    style={styles.formInput}
                  >
                    <option value="">Select method...</option>
                    <option value="Cash">Cash</option>
                    <option value="Bank Transfer">Bank Transfer</option>
                    <option value="Credit Card">Credit Card</option>
                    <option value="Check">Check</option>
                    <option value="Other">Other</option>
                  </select>
                </div>
                <div style={styles.formGroup}>
                  <label style={styles.formLabel}>Reference</label>
                  <input
                    type="text"
                    name="reference"
                    value={paymentFormData.reference}
                    onChange={handlePaymentFormChange}
                    placeholder="Transaction ID, check number, etc."
                    style={styles.formInput}
                  />
                </div>
                {recordPaymentMutation.error && (
                  <div style={styles.errorBanner}>
                    {recordPaymentMutation.error instanceof Error
                      ? recordPaymentMutation.error.message
                      : 'Failed to record payment'}
                  </div>
                )}
                <div style={styles.modalActions}>
                  <button
                    type="button"
                    onClick={handleCancelRecordPayment}
                    style={{ ...styles.modalButton, ...styles.modalButtonSecondary }}
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isAnyMutationPending || !paymentFormData.amount || !paymentFormData.method}
                    style={{ ...styles.modalButton, ...styles.modalButtonSuccess }}
                  >
                    {recordPaymentMutation.isPending ? 'Recording...' : 'Record Payment'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        <div style={styles.detailsCard}>
          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Invoice Number</span>
            <span style={styles.detailValue}>{invoice.invoiceNumberText}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Status</span>
            <span style={getStatusBadgeStyle(invoice.status)}>
              {invoice.status}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Created</span>
            <span style={styles.detailValue}>{formatDateTime(invoice.createdUtc)}</span>
          </div>

          {invoice.issuedUtc && (
            <div style={styles.detailRow}>
              <span style={styles.detailLabel}>Issued</span>
              <span style={styles.detailValue}>{formatDateTime(invoice.issuedUtc)}</span>
            </div>
          )}

          {invoice.sentUtc && (
            <div style={styles.detailRow}>
              <span style={styles.detailLabel}>Sent</span>
              <span style={styles.detailValue}>{formatDateTime(invoice.sentUtc)}</span>
            </div>
          )}

          {invoice.paidUtc && (
            <div style={styles.detailRow}>
              <span style={styles.detailLabel}>Paid</span>
              <span style={styles.detailValue}>{formatDateTime(invoice.paidUtc)}</span>
            </div>
          )}

          {invoice.voidedUtc && (
            <div style={styles.detailRow}>
              <span style={styles.detailLabel}>Voided</span>
              <span style={styles.detailValue}>{formatDateTime(invoice.voidedUtc)}</span>
            </div>
          )}

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Booking ID</span>
            <span style={styles.detailValue}>{invoice.bookingId.substring(0, 8)}...</span>
          </div>

          <div style={styles.divider}></div>

          <h3 style={styles.sectionTitle}>Items</h3>
          <div style={styles.itemsTableContainer}>
            <table style={styles.itemsTable}>
              <thead>
                <tr>
                  <th style={styles.itemsTh}>Description</th>
                  <th style={styles.itemsTh}>Quantity</th>
                  <th style={styles.itemsTh}>Unit Price</th>
                  <th style={styles.itemsTh}>Line Total</th>
                </tr>
              </thead>
              <tbody>
                {invoice.items.map((item) => (
                  <tr key={item.id}>
                    <td style={styles.itemsTd}>{item.description}</td>
                    <td style={styles.itemsTd}>{item.quantity}</td>
                    <td style={styles.itemsTd}>{formatCurrency(item.unitPrice, invoice.currency)}</td>
                    <td style={styles.itemsTd}>{formatCurrency(item.lineTotal, invoice.currency)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div style={styles.divider}></div>

          <h3 style={styles.sectionTitle}>Financial Summary</h3>
          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Subtotal</span>
            <span style={styles.detailValue}>{formatCurrency(invoice.subtotalAmount, invoice.currency)}</span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Amount Paid</span>
            <span style={{ ...styles.detailValue, color: '#28a745' }}>
              {formatCurrency(invoice.amountPaid, invoice.currency)}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Amount Due</span>
            <span style={{ ...styles.detailValue, ...styles.amountDueValue }}>
              {formatCurrency(invoice.amountDue, invoice.currency)}
            </span>
          </div>

          <div style={styles.detailRow}>
            <span style={styles.detailLabel}>Paid in Full</span>
            <span style={styles.detailValue}>{invoice.isPaid ? 'Yes' : 'No'}</span>
          </div>

          <div style={styles.divider}></div>

          <h3 style={styles.sectionTitle}>Payments</h3>
          {isLoadingPayments ? (
            <div style={styles.loading}>Loading payments...</div>
          ) : payments && payments.length > 0 ? (
            <div style={styles.paymentsTableContainer}>
              <table style={styles.paymentsTable}>
                <thead>
                  <tr>
                    <th style={styles.paymentsTh}>Date</th>
                    <th style={styles.paymentsTh}>Amount</th>
                    <th style={styles.paymentsTh}>Method</th>
                    <th style={styles.paymentsTh}>Reference</th>
                  </tr>
                </thead>
                <tbody>
                  {payments.map((payment) => (
                    <tr key={payment.id}>
                      <td style={styles.paymentsTd}>{formatDateTime(payment.paidUtc)}</td>
                      <td style={styles.paymentsTd}>{formatCurrency(payment.amount, invoice.currency)}</td>
                      <td style={styles.paymentsTd}>{payment.method}</td>
                      <td style={styles.paymentsTd}>{payment.reference || '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div style={styles.emptyState}>
              <p style={styles.emptyStateText}>No payments recorded yet.</p>
            </div>
          )}
        </div>
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
    alignItems: 'center',
  },
  actionButton: {
    padding: '8px 16px',
    fontSize: '14px',
    borderRadius: '4px',
    cursor: 'pointer',
    border: 'none',
    fontWeight: 500,
  },
  primaryButton: {
    backgroundColor: '#007bff',
    color: 'white',
  },
  secondaryButton: {
    backgroundColor: '#6c757d',
    color: 'white',
  },
  successButton: {
    backgroundColor: '#28a745',
    color: 'white',
  },
  dangerButton: {
    backgroundColor: '#dc3545',
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
  amountDueValue: {
    fontWeight: 'bold',
    color: '#c33',
  },
  statusBadge: {
    display: 'inline-block',
    padding: '4px 12px',
    borderRadius: '4px',
    fontSize: '14px',
    fontWeight: '600',
  },
  statusDraft: {
    backgroundColor: '#e2e3e5',
    color: '#383d41',
  },
  statusIssued: {
    backgroundColor: '#fff3cd',
    color: '#856404',
  },
  statusPaid: {
    backgroundColor: '#d4edda',
    color: '#155724',
  },
  statusVoid: {
    backgroundColor: '#f8d7da',
    color: '#721c24',
  },
  divider: {
    height: '1px',
    backgroundColor: '#e0e0e0',
    margin: '24px 0',
  },
  sectionTitle: {
    fontSize: '16px',
    fontWeight: 'bold',
    color: '#333',
    margin: '0 0 16px 0',
  },
  itemsTableContainer: {
    backgroundColor: '#f8f9fa',
    borderRadius: '4px',
    overflow: 'hidden',
  },
  itemsTable: {
    width: '100%',
    borderCollapse: 'collapse',
  },
  itemsTh: {
    padding: '10px 12px',
    textAlign: 'left',
    fontSize: '13px',
    fontWeight: '600',
    color: '#666',
    borderBottom: '1px solid #e0e0e0',
    backgroundColor: '#f0f0f0',
  },
  itemsTd: {
    padding: '10px 12px',
    fontSize: '13px',
    color: '#333',
    borderBottom: '1px solid #e0e0e0',
  },
  // Modal styles
  modalOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
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
  modalMessage: {
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
  modalButton: {
    padding: '8px 16px',
    fontSize: '14px',
    borderRadius: '4px',
    cursor: 'pointer',
    border: 'none',
    fontWeight: 500,
  },
  modalButtonSecondary: {
    backgroundColor: '#6c757d',
    color: 'white',
  },
  modalButtonDanger: {
    backgroundColor: '#dc3545',
    color: 'white',
  },
  modalButtonSuccess: {
    backgroundColor: '#28a745',
    color: 'white',
  },
  // Form styles
  formGroup: {
    marginBottom: '16px',
  },
  formLabel: {
    display: 'block',
    fontSize: '14px',
    fontWeight: 600,
    color: '#333',
    marginBottom: '4px',
  },
  formInput: {
    width: '100%',
    padding: '8px 12px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    boxSizing: 'border-box',
  },
  // Payments section styles
  paymentsTableContainer: {
    backgroundColor: '#f8f9fa',
    borderRadius: '4px',
    overflow: 'hidden',
  },
  paymentsTable: {
    width: '100%',
    borderCollapse: 'collapse',
  },
  paymentsTh: {
    padding: '10px 12px',
    textAlign: 'left',
    fontSize: '13px',
    fontWeight: 600,
    color: '#666',
    borderBottom: '1px solid #e0e0e0',
    backgroundColor: '#f0f0f0',
  },
  paymentsTd: {
    padding: '10px 12px',
    fontSize: '13px',
    color: '#333',
    borderBottom: '1px solid #e0e0e0',
  },
  emptyState: {
    padding: '24px',
    textAlign: 'center',
    backgroundColor: '#f8f9fa',
    borderRadius: '4px',
  },
  emptyStateText: {
    margin: 0,
    color: '#666',
    fontSize: '14px',
  },
}
