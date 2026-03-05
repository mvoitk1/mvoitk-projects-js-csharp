import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { SidebarNav } from './SidebarNav'

interface TenantShellProps {
  companySlug: string | undefined
  children: React.ReactNode
}

/**
 * TenantShell - Layout for employee/tenant mode
 * 
 * Features:
 * - Fixed left sidebar with navigation
 * - Top workspace header with company identifier, customer view, switch company, logout
 * - Main content area
 */
export function TenantShell({ companySlug, children }: TenantShellProps) {
  const navigate = useNavigate()
  const { logout } = useAuth()
  
  const handleLogout = () => {
    logout()
  }
  
  const handleSwitchCompany = () => {
    // Clear last company selection to force company selection page
    localStorage.removeItem('lastCompanySlug')
    navigate('/select-company')
  }

  const handleCustomerView = () => {
    navigate('/customer')
  }
  
  return (
    <div style={styles.container}>
      {/* Sidebar */}
      <aside style={styles.sidebar}>
        <div style={styles.sidebarHeader}>
          <span style={styles.logo}>VenuePlatform</span>
        </div>
        <SidebarNav companySlug={companySlug || ''} />
      </aside>
      
      {/* Main content area */}
      <div style={styles.mainArea}>
        {/* Workspace header */}
        <header style={styles.header}>
          <div style={styles.headerLeft}>
            <span style={styles.companyLabel}>Company:</span>
            <span style={styles.companySlug}>/{companySlug || 'unknown'}</span>
          </div>
          <div style={styles.headerRight}>
            <button 
              onClick={handleCustomerView}
              style={styles.customerViewButton}
            >
              Customer View
            </button>
            <button 
              onClick={handleSwitchCompany}
              style={styles.switchButton}
            >
              Switch Company
            </button>
            <button 
              onClick={handleLogout}
              style={styles.logoutButton}
            >
              Logout
            </button>
          </div>
        </header>
        
        {/* Page content */}
        <main style={styles.content}>
          {children}
        </main>
      </div>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    display: 'flex',
    minHeight: '100vh',
  },
  sidebar: {
    width: '240px',
    backgroundColor: '#f8f9fa',
    borderRight: '1px solid #e0e0e0',
    display: 'flex',
    flexDirection: 'column',
    position: 'fixed',
    top: 0,
    left: 0,
    bottom: 0,
    zIndex: 100,
  },
  sidebarHeader: {
    padding: '16px 20px',
    borderBottom: '1px solid #e0e0e0',
  },
  logo: {
    fontSize: '18px',
    fontWeight: 'bold',
    color: '#333',
  },
  mainArea: {
    flex: 1,
    marginLeft: '240px',
    display: 'flex',
    flexDirection: 'column',
    minHeight: '100vh',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0 24px',
    height: '60px',
    backgroundColor: 'white',
    borderBottom: '1px solid #e0e0e0',
    boxShadow: '0 1px 3px rgba(0,0,0,0.05)',
    position: 'sticky',
    top: 0,
    zIndex: 50,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  companyLabel: {
    fontSize: '14px',
    color: '#666',
  },
  companySlug: {
    fontSize: '14px',
    fontWeight: '600',
    color: '#333',
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
  },
  customerViewButton: {
    padding: '8px 16px',
    backgroundColor: '#f3f4f6',
    color: '#374151',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '14px',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
  },
  switchButton: {
    padding: '8px 16px',
    backgroundColor: 'transparent',
    color: '#2563eb',
    border: '1px solid #2563eb',
    borderRadius: '4px',
    fontSize: '14px',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
  },
  logoutButton: {
    padding: '8px 16px',
    backgroundColor: 'transparent',
    color: '#666',
    border: '1px solid #ddd',
    borderRadius: '4px',
    fontSize: '14px',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
  },
  content: {
    flex: 1,
    padding: '24px',
    backgroundColor: '#f5f5f5',
  },
}
