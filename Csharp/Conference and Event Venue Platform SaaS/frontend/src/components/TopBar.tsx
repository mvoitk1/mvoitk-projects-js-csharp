import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

export function TopBar() {
  const { companySlug: authCompanySlug, logout } = useAuth()
  const { companySlug: urlCompanySlug } = useParams<{ companySlug: string }>()
  const companySlug = urlCompanySlug || authCompanySlug
  
  const handleLogout = () => {
    logout()
  }
  
  return (
    <header style={styles.header}>
      <div style={styles.left}>
        <Link to={companySlug ? `/${companySlug}/dashboard` : '/'} style={styles.logoLink}>
          <span style={styles.logo}>VenuePlatform</span>
        </Link>
        {companySlug && (
          <span style={styles.companySlug}>/{companySlug}</span>
        )}
        
        {companySlug && (
          <nav style={styles.nav}>
            <Link to={`/${companySlug}/dashboard`} style={styles.navLink}>
              Dashboard
            </Link>
            <Link to={`/${companySlug}/spaces`} style={styles.navLink}>
              Spaces
            </Link>
            <Link to={`/${companySlug}/clients`} style={styles.navLink}>
              Clients
            </Link>
            <Link to={`/${companySlug}/bookings`} style={styles.navLink}>
              Bookings
            </Link>
          </nav>
        )}
      </div>
      
      <div style={styles.right}>
        <button onClick={handleLogout} style={styles.logoutButton}>
          Logout
        </button>
      </div>
    </header>
  )
}

const styles: Record<string, React.CSSProperties> = {
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0 24px',
    height: '60px',
    backgroundColor: 'white',
    borderBottom: '1px solid #e0e0e0',
    boxShadow: '0 1px 3px rgba(0,0,0,0.05)',
  },
  left: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  logoLink: {
    textDecoration: 'none',
  },
  logo: {
    fontSize: '18px',
    fontWeight: 'bold',
    color: '#333',
  },
  companySlug: {
    fontSize: '14px',
    color: '#666',
  },
  nav: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    marginLeft: '24px',
    paddingLeft: '24px',
    borderLeft: '1px solid #e0e0e0',
  },
  navLink: {
    padding: '8px 16px',
    color: '#666',
    textDecoration: 'none',
    fontSize: '14px',
    borderRadius: '4px',
    transition: 'background-color 0.2s, color 0.2s',
  },
  right: {
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
}