import { useQuery } from '@tanstack/react-query'
import { Layout } from '../components/Layout'
import { PlanUsageWidget } from '../components/PlanUsageWidget'
import { api } from '../api/apiClient'
import { PlanUsageResponse } from '../types/apiTypes'
import { useCompanySlug } from '../hooks/useCompanySlug'

async function fetchPlanUsage(companySlug: string): Promise<PlanUsageResponse> {
  return api.get<PlanUsageResponse>(`/${companySlug}/billing/plan`)
}

export function DashboardPage() {
  const companySlug = useCompanySlug()
  
  const { data, isLoading, error } = useQuery({
    queryKey: ['planUsage', companySlug],
    queryFn: () => fetchPlanUsage(companySlug),
  })
  
  return (
    <Layout>
      <div style={styles.container}>
        <h1 style={styles.title}>Dashboard</h1>
        
        {isLoading && (
          <div style={styles.loading}>Loading plan information...</div>
        )}
        
        {error && (
          <div style={styles.error}>
            Failed to load plan information. Please try again.
          </div>
        )}
        
        {data && (
          <div style={styles.content}>
            <div style={styles.welcome}>
              <h2 style={styles.companyName}>{data.companyName}</h2>
              <p style={styles.planBadge}>{data.plan} Plan</p>
            </div>
            
            <PlanUsageWidget
              currentSpaces={data.currentSpaces}
              maxSpaces={data.maxSpaces}
              currentBookings={data.currentBookingsThisMonth}
              maxBookings={data.maxBookingsPerMonth}
            />
          </div>
        )}
      </div>
    </Layout>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '24px',
    maxWidth: '800px',
  },
  title: {
    fontSize: '28px',
    fontWeight: 'bold',
    marginBottom: '24px',
    color: '#333',
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#666',
  },
  error: {
    padding: '16px',
    backgroundColor: '#fee',
    color: '#c33',
    borderRadius: '4px',
    marginBottom: '16px',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '24px',
  },
  welcome: {
    backgroundColor: 'white',
    padding: '24px',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
  },
  companyName: {
    fontSize: '20px',
    fontWeight: '600',
    marginBottom: '8px',
    color: '#333',
  },
  planBadge: {
    display: 'inline-block',
    padding: '4px 12px',
    backgroundColor: '#007bff',
    color: 'white',
    borderRadius: '12px',
    fontSize: '12px',
    fontWeight: '500',
    textTransform: 'uppercase',
  },
}