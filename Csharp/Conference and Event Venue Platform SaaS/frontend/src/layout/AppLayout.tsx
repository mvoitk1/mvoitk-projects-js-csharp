import { useEffect } from 'react'
import { useParams, useLocation } from 'react-router-dom'
import { TenantShell } from './TenantShell'
import { CustomerShell } from './CustomerShell'
import { useAuth } from '../auth/useAuth'

interface AppLayoutProps {
  children: React.ReactNode
  mode?: 'tenant' | 'customer' | 'auto'
}

/**
 * AppLayout - Main application layout component
 * 
 * Automatically detects the mode based on route or uses explicit mode prop:
 * - tenant: Shows sidebar navigation + workspace header (for /:companySlug/* routes)
 * - customer: Shows simple header (for /customer, /select-company, /become-a-venue)
 * - auto: Detects based on URL pattern
 */
export function AppLayout({ children, mode = 'auto' }: AppLayoutProps) {
  const { companySlug } = useParams<{ companySlug: string }>()
  const location = useLocation()
  const { companySlug: authCompanySlug, setCompanySlug } = useAuth()
  
  // Determine mode automatically if not explicitly provided
  const effectiveMode = mode === 'auto' 
    ? (companySlug ? 'tenant' : 'customer')
    : mode
  
  // Check if we're on a route that should use customer shell
  const isCustomerRoute = ['/customer', '/select-company', '/become-a-venue', '/session'].includes(location.pathname)

  useEffect(() => {
    if (companySlug && companySlug !== authCompanySlug) {
      setCompanySlug(companySlug)
    }
  }, [authCompanySlug, companySlug, setCompanySlug])
  
  // If explicitly on a customer route, use customer shell even if somehow we have a companySlug
  if (isCustomerRoute || effectiveMode === 'customer') {
    return <CustomerShell>{children}</CustomerShell>
  }
  
  return <TenantShell companySlug={companySlug}>{children}</TenantShell>
}
