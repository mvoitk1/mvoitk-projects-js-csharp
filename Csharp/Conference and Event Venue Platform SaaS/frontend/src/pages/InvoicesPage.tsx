import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getInvoices } from '../api/invoicesApi'
import { useCompanySlug } from '../hooks/useCompanySlug'
import type { InvoiceStatus } from '../types/apiTypes'

export function InvoicesPage() {
  const companySlug = useCompanySlug()
  const navigate = useNavigate()

  // Query for invoices list
  const { data: invoices, isLoading, isError, error } = useQuery({
    queryKey: ['invoices', companySlug],
    queryFn: () => getInvoices(companySlug),
  })

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
    return 'Failed to load invoices. Please try again.'
  }

  return (
    <div style={styles.container}>
        <div style={styles.header}>
          <h1 style={styles.title}>Invoices</h1>
        </div>

        {isError && (
          <div style={styles.errorBanner}>
            {getErrorMessage()}
          </div>
        )}

        {isLoading ? (
          <div style={styles.loading}>Loading invoices...</div>
        ) : (
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Invoice #</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th}>Total</th>
                  <th style={styles.th}>Amount Due</th>
                  <th style={styles.th}>Created</th>
                  <th style={styles.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {invoices?.length === 0 ? (
                  <tr>
                    <td colSpan={6} style={styles.emptyCell}>
                      No invoices yet.
                    </td>
                  </tr>
                ) : (
                  invoices?.map((invoice) => (
                    <tr key={invoice.id} style={styles.tr}>
                      <td style={styles.td}>{invoice.invoiceNumberText}</td>
                      <td style={styles.td}>
                        <span style={getStatusBadgeStyle(invoice.status)}>
                          {invoice.status}
                        </span>
                      </td>
                      <td style={styles.td}>{formatCurrency(invoice.subtotalAmount, invoice.currency)}</td>
                      <td style={styles.td}>{formatCurrency(invoice.amountDue, invoice.currency)}</td>
                      <td style={styles.td}>{formatDateTime(invoice.createdUtc)}</td>
                      <td style={styles.td}>
                        <div style={styles.actions}>
                          <button
                            onClick={() => navigate(`/${companySlug}/invoices/${invoice.id}`)}
                            style={styles.actionButton}
                          >
                            View
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
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
}
