import { NavLink } from 'react-router-dom'

interface SidebarNavProps {
  companySlug: string
}

interface NavItem {
  to: string
  label: string
  icon?: string
}

/**
 * SidebarNav - Navigation links for tenant mode
 * 
 * Uses NavLink for active styling.
 * Links are prefixed with companySlug for tenant routes.
 */
export function SidebarNav({ companySlug }: SidebarNavProps) {
  const navItems: NavItem[] = [
    { to: `/${companySlug}/dashboard`, label: 'Dashboard', icon: '📊' },
    { to: `/${companySlug}/bookings`, label: 'Bookings', icon: '📅' },
    { to: `/${companySlug}/spaces`, label: 'Spaces', icon: '🏢' },
    { to: `/${companySlug}/clients`, label: 'Clients', icon: '👥' },
    { to: `/${companySlug}/invoices`, label: 'Invoices', icon: '📄' },
    { to: `/${companySlug}/reports/revenue`, label: 'Reports', icon: '📈' },
    { to: `/${companySlug}/billing`, label: 'Billing', icon: '💳' },
  ]

  return (
    <nav style={styles.nav}>
      {navItems.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          style={({ isActive }) => ({
            ...styles.navLink,
            ...(isActive ? styles.activeNavLink : {}),
          })}
        >
          <span style={styles.icon}>{item.icon}</span>
          <span style={styles.label}>{item.label}</span>
        </NavLink>
      ))}
    </nav>
  )
}

const styles: Record<string, React.CSSProperties> = {
  nav: {
    display: 'flex',
    flexDirection: 'column',
    padding: '12px',
    gap: '4px',
  },
  navLink: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '10px 14px',
    color: '#666',
    textDecoration: 'none',
    fontSize: '14px',
    borderRadius: '6px',
    transition: 'background-color 0.2s, color 0.2s',
  },
  activeNavLink: {
    backgroundColor: '#e8f0fe',
    color: '#2563eb',
    fontWeight: '500',
  },
  icon: {
    fontSize: '16px',
    width: '20px',
    textAlign: 'center',
  },
  label: {
    flex: 1,
  },
}
