import { useQuery } from '@tanstack/react-query'
import { getBillingPlan } from '../api/billingApi'
import { useCompanySlug } from '../hooks/useCompanySlug'
import type { CompanyPlan } from '../types/apiTypes'

export function BillingPlanPage() {
  const companySlug = useCompanySlug()

  // Query for billing plan
  const { data: planData, isLoading, isError, error } = useQuery({
    queryKey: ['billing-plan', companySlug],
    queryFn: () => getBillingPlan(companySlug),
  })

  const formatDate = (utcString: string | undefined) => {
    if (!utcString) return 'N/A'
    const date = new Date(utcString)
    return date.toLocaleDateString()
  }

  const getPlanBadgeStyle = (plan: CompanyPlan) => {
    switch (plan) {
      case 'Enterprise':
        return { ...styles.planBadge, ...styles.planEnterprise }
      case 'Professional':
        return { ...styles.planBadge, ...styles.planProfessional }
      case 'Starter':
        return { ...styles.planBadge, ...styles.planStarter }
      case 'Free':
      default:
        return { ...styles.planBadge, ...styles.planFree }
    }
  }

  const getErrorMessage = () => {
    if (error instanceof Error) {
      return error.message
    }
    return 'Failed to load billing plan. Please try again.'
  }

  const calculateUsagePercent = (current: number, max: number) => {
    if (max === 0) return 0
    return Math.min(Math.round((current / max) * 100), 100)
  }

  return (
    
      <div style={styles.container}>
        <div style={styles.header}>
          <h1 style={styles.title}>Billing Plan</h1>
        </div>

        {isError && (
          <div style={styles.errorBanner}>
            {getErrorMessage()}
          </div>
        )}

        {isLoading ? (
          <div style={styles.loading}>Loading billing plan...</div>
        ) : planData ? (
          <div style={styles.content}>
            {/* Plan Info Card */}
            <div style={styles.card}>
              <div style={styles.cardHeader}>
                <h2 style={styles.cardTitle}>Current Plan</h2>
                <span style={getPlanBadgeStyle(planData.plan)}>
                  {planData.plan}
                </span>
              </div>
              <div style={styles.cardContent}>
                <div style={styles.infoRow}>
                  <span style={styles.infoLabel}>Company:</span>
                  <span style={styles.infoValue}>{planData.companyName}</span>
                </div>
                <div style={styles.infoRow}>
                  <span style={styles.infoLabel}>Billing Period:</span>
                  <span style={styles.infoValue}>
                    {formatDate(planData.monthStartUtc)} - {formatDate(planData.monthEndUtc)}
                  </span>
                </div>
              </div>
            </div>

            {/* Usage Cards */}
            <div style={styles.usageGrid}>
              {/* Spaces Usage */}
              <div style={styles.card}>
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Spaces</h3>
                </div>
                <div style={styles.cardContent}>
                  <div style={styles.usageNumbers}>
                    <span style={styles.usageCurrent}>{planData.currentSpaces}</span>
                    <span style={styles.usageSeparator}>/</span>
                    <span style={styles.usageMax}>{planData.maxSpaces}</span>
                  </div>
                  <div style={styles.progressBarContainer}>
                    <div
                      style={{
                        ...styles.progressBar,
                        width: `${calculateUsagePercent(planData.currentSpaces, planData.maxSpaces)}%`,
                        backgroundColor: planData.currentSpaces >= planData.maxSpaces ? '#dc3545' : '#28a745',
                      }}
                    />
                  </div>
                  <p style={styles.usageLabel}>spaces used</p>
                </div>
              </div>

              {/* Bookings Usage */}
              <div style={styles.card}>
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Bookings This Month</h3>
                </div>
                <div style={styles.cardContent}>
                  <div style={styles.usageNumbers}>
                    <span style={styles.usageCurrent}>{planData.currentBookingsThisMonth}</span>
                    <span style={styles.usageSeparator}>/</span>
                    <span style={styles.usageMax}>{planData.maxBookingsPerMonth}</span>
                  </div>
                  <div style={styles.progressBarContainer}>
                    <div
                      style={{
                        ...styles.progressBar,
                        width: `${calculateUsagePercent(planData.currentBookingsThisMonth, planData.maxBookingsPerMonth)}%`,
                        backgroundColor: planData.currentBookingsThisMonth >= planData.maxBookingsPerMonth ? '#dc3545' : '#28a745',
                      }}
                    />
                  </div>
                  <p style={styles.usageLabel}>bookings used this month</p>
                </div>
              </div>
            </div>
          </div>
        ) : null}
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
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '24px',
  },
  card: {
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
    overflow: 'hidden',
  },
  cardHeader: {
    padding: '16px 20px',
    borderBottom: '1px solid #e0e0e0',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  cardTitle: {
    fontSize: '18px',
    fontWeight: '600',
    color: '#333',
    margin: 0,
  },
  cardContent: {
    padding: '20px',
  },
  planBadge: {
    display: 'inline-block',
    padding: '6px 12px',
    borderRadius: '4px',
    fontSize: '14px',
    fontWeight: '600',
  },
  planFree: {
    backgroundColor: '#e2e3e5',
    color: '#383d41',
  },
  planStarter: {
    backgroundColor: '#d1ecf1',
    color: '#0c5460',
  },
  planProfessional: {
    backgroundColor: '#d4edda',
    color: '#155724',
  },
  planEnterprise: {
    backgroundColor: '#f8d7da',
    color: '#721c24',
  },
  infoRow: {
    display: 'flex',
    marginBottom: '12px',
  },
  infoLabel: {
    width: '120px',
    fontWeight: '600',
    color: '#666',
  },
  infoValue: {
    color: '#333',
  },
  usageGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: '24px',
  },
  usageNumbers: {
    display: 'flex',
    alignItems: 'baseline',
    justifyContent: 'center',
    gap: '8px',
    marginBottom: '16px',
  },
  usageCurrent: {
    fontSize: '48px',
    fontWeight: 'bold',
    color: '#333',
  },
  usageSeparator: {
    fontSize: '24px',
    color: '#999',
  },
  usageMax: {
    fontSize: '24px',
    color: '#666',
  },
  progressBarContainer: {
    height: '8px',
    backgroundColor: '#e0e0e0',
    borderRadius: '4px',
    overflow: 'hidden',
    marginBottom: '8px',
  },
  progressBar: {
    height: '100%',
    borderRadius: '4px',
    transition: 'width 0.3s ease',
  },
  usageLabel: {
    textAlign: 'center',
    color: '#666',
    fontSize: '14px',
    margin: 0,
  },
}
