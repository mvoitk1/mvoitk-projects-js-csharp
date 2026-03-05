import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

interface CustomerShellProps {
  children: React.ReactNode
}

/**
 * CustomerShell - Layout for customer mode (no company/tenant)
 * 
 * Features:
 * - Simple centered container
 * - Header with navigation links and logout
 * - Clean card-based content area
 */
export function CustomerShell({ children }: CustomerShellProps) {
  const { logout } = useAuth()
  
  const handleLogout = () => {
    logout()
  }
  
  return (
    <div style={styles.container}>
      {/* Header */}
      <header style={styles.header}>
        <div style={styles.headerLeft}>
          <Link to="/customer" style={styles.logoLink}>
            <span style={styles.logo}>VenuePlatform</span>
          </Link>
          <span style={styles.modeLabel}>Customer</span>
        </div>
        
        <nav style={styles.nav}>
          <Link to="/customer" style={styles.navLink}>
            Home
          </Link>
          <Link to="/customer/venues" style={styles.navLink}>
            Venues
          </Link>
          <Link to="/select-company" style={styles.navLink}>
            Select Company
          </Link>
          <Link to="/become-a-venue" style={styles.navLink}>
            Become a Venue
          </Link>
        </nav>
        
        <div style={styles.headerRight}>
          <button 
            onClick={handleLogout}
            style={styles.logoutButton}
          >
            Logout
          </button>
        </div>
      </header>
      
      {/* Main content */}
      <main style={styles.main}>
        {children}
      </main>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    minHeight: '100vh',
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: '#f5f5f5',
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
    gap: '12px',
  },
  logoLink: {
    textDecoration: 'none',
  },
  logo: {
    fontSize: '18px',
    fontWeight: 'bold',
    color: '#333',
  },
  modeLabel: {
    fontSize: '12px',
    fontWeight: '500',
    color: '#2563eb',
    backgroundColor: '#eff6ff',
    padding: '4px 10px',
    borderRadius: '12px',
    textTransform: 'uppercase',
  },
  nav: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  navLink: {
    padding: '8px 16px',
    color: '#666',
    textDecoration: 'none',
    fontSize: '14px',
    borderRadius: '4px',
    transition: 'background-color 0.2s, color 0.2s',
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
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
  main: {
    flex: 1,
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'flex-start',
    padding: '40px 20px',
  },
}
