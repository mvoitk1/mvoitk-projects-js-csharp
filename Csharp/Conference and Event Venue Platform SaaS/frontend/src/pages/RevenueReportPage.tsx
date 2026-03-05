import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Layout } from '../components/Layout'
import { getRevenueReport, getRevenueDaily } from '../api/reportsApi'
import { useCompanySlug } from '../hooks/useCompanySlug'

function formatDateInput(date: Date): string {
  return date.toISOString().split('T')[0]
}

function formatDateDisplay(dateString: string): string {
  return new Date(dateString).toLocaleDateString()
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

export function RevenueReportPage() {
  const companySlug = useCompanySlug()
  
  // Default date range: last 30 days
  const today = useMemo(() => new Date(), [])
  const thirtyDaysAgo = useMemo(() => {
    const d = new Date()
    d.setDate(d.getDate() - 30)
    return d
  }, [])
  
  const [fromDate, setFromDate] = useState(formatDateInput(thirtyDaysAgo))
  const [toDate, setToDate] = useState(formatDateInput(today))
  
  // Convert dates to UTC ISO strings for API
  const fromUtc = useMemo(() => {
    const date = new Date(fromDate)
    date.setHours(0, 0, 0, 0)
    return date.toISOString()
  }, [fromDate])
  
  const toUtc = useMemo(() => {
    const date = new Date(toDate)
    date.setHours(23, 59, 59, 999)
    return date.toISOString()
  }, [toDate])
  
  // Query for revenue summary
  const {
    data: summaryData,
    isLoading: summaryLoading,
    isError: summaryError,
  } = useQuery({
    queryKey: ['revenueReport', companySlug, fromUtc, toUtc],
    queryFn: () => getRevenueReport(companySlug, fromUtc, toUtc),
  })
  
  // Query for daily breakdown
  const {
    data: dailyData,
    isLoading: dailyLoading,
    isError: dailyError,
  } = useQuery({
    queryKey: ['revenueDaily', companySlug, fromUtc, toUtc],
    queryFn: () => getRevenueDaily(companySlug, fromUtc, toUtc),
  })
  
  const isLoading = summaryLoading || dailyLoading
  const hasError = summaryError || dailyError
  
  // Calculate derived values
  const averageInvoice = useMemo(() => {
    if (!summaryData || summaryData.paymentCount === 0) return 0
    return summaryData.totalPayments / summaryData.paymentCount
  }, [summaryData])
  
  return (
    <Layout>
      <div style={styles.container}>
        <h1 style={styles.title}>Revenue Report</h1>
        
        {/* Date Range Selector */}
        <div style={styles.dateSelector}>
          <div style={styles.dateField}>
            <label style={styles.label}>From</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              style={styles.dateInput}
            />
          </div>
          <div style={styles.dateField}>
            <label style={styles.label}>To</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              style={styles.dateInput}
            />
          </div>
        </div>
        
        {/* Error State */}
        {hasError && (
          <div style={styles.errorBanner}>
            Failed to load revenue data. Please try again.
          </div>
        )}
        
        {/* Loading State */}
        {isLoading ? (
          <div style={styles.loading}>Loading revenue report...</div>
        ) : (
          <>
            {/* Summary Cards */}
            {summaryData && (
              <div style={styles.summaryCards}>
                <div style={styles.summaryCard}>
                  <div style={styles.summaryLabel}>Total Revenue</div>
                  <div style={styles.summaryValue}>
                    {formatCurrency(summaryData.totalPayments)}
                  </div>
                </div>
                <div style={styles.summaryCard}>
                  <div style={styles.summaryLabel}>Invoice Count</div>
                  <div style={styles.summaryValue}>
                    {summaryData.paymentCount}
                  </div>
                </div>
                <div style={styles.summaryCard}>
                  <div style={styles.summaryLabel}>Average Invoice</div>
                  <div style={styles.summaryValue}>
                    {formatCurrency(averageInvoice)}
                  </div>
                </div>
              </div>
            )}
            
            {/* Daily Breakdown Table */}
            {dailyData && dailyData.days.length > 0 && (
              <div style={styles.tableContainer}>
                <h2 style={styles.tableTitle}>Daily Breakdown</h2>
                <table style={styles.table}>
                  <thead>
                    <tr>
                      <th style={styles.th}>Date</th>
                      <th style={styles.th}>Revenue</th>
                      <th style={styles.th}>Payment Count</th>
                    </tr>
                  </thead>
                  <tbody>
                    {dailyData.days.map((day, index) => (
                      <tr key={index} style={styles.tr}>
                        <td style={styles.td}>
                          {formatDateDisplay(day.dateUtc)}
                        </td>
                        <td style={styles.td}>
                          {formatCurrency(day.totalPayments)}
                        </td>
                        <td style={styles.td}>
                          {day.paymentCount}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            
            {/* Empty State */}
            {dailyData && dailyData.days.length === 0 && !hasError && (
              <div style={styles.emptyState}>
                No revenue data for the selected date range.
              </div>
            )}
          </>
        )}
      </div>
    </Layout>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '1000px',
  },
  title: {
    fontSize: '28px',
    fontWeight: 'bold',
    color: '#333',
    marginBottom: '24px',
  },
  dateSelector: {
    display: 'flex',
    gap: '16px',
    marginBottom: '24px',
    padding: '16px',
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
  },
  dateField: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  label: {
    fontSize: '12px',
    fontWeight: '600',
    color: '#666',
    textTransform: 'uppercase' as const,
  },
  dateInput: {
    padding: '8px 12px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
  },
  errorBanner: {
    padding: '12px 16px',
    backgroundColor: '#fee',
    color: '#c33',
    borderRadius: '4px',
    marginBottom: '16px',
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  summaryCards: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '16px',
    marginBottom: '24px',
  },
  summaryCard: {
    padding: '20px',
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
  },
  summaryLabel: {
    fontSize: '12px',
    fontWeight: '600',
    color: '#666',
    textTransform: 'uppercase' as const,
    marginBottom: '8px',
  },
  summaryValue: {
    fontSize: '24px',
    fontWeight: 'bold',
    color: '#333',
  },
  tableContainer: {
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    overflow: 'hidden',
  },
  tableTitle: {
    padding: '16px 20px',
    margin: 0,
    fontSize: '18px',
    fontWeight: '600',
    color: '#333',
    borderBottom: '1px solid #e0e0e0',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
  },
  th: {
    padding: '12px 20px',
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
    padding: '12px 20px',
    fontSize: '14px',
    color: '#333',
  },
  emptyState: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
  },
}
