import { ReactNode } from 'react'

interface PageHeaderProps {
  title: string
  subtitle?: string
  actions?: ReactNode
}

/**
 * PageHeader - Reusable page header component
 * 
 * Provides consistent header styling across pages with:
 * - Title (required)
 * - Subtitle (optional)
 * - Action buttons/content (optional)
 */
export function PageHeader({ title, subtitle, actions }: PageHeaderProps) {
  return (
    <div style={styles.container}>
      <div style={styles.left}>
        <h1 style={styles.title}>{title}</h1>
        {subtitle && <p style={styles.subtitle}>{subtitle}</p>}
      </div>
      {actions && <div style={styles.actions}>{actions}</div>}
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    marginBottom: '24px',
    gap: '16px',
  },
  left: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  title: {
    fontSize: '28px',
    fontWeight: 'bold',
    color: '#333',
    margin: 0,
  },
  subtitle: {
    fontSize: '14px',
    color: '#666',
    margin: 0,
  },
  actions: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flexShrink: 0,
  },
}
