interface PlanUsageWidgetProps {
  currentSpaces: number
  maxSpaces: number
  currentBookings: number
  maxBookings: number
}

export function PlanUsageWidget({
  currentSpaces,
  maxSpaces,
  currentBookings,
  maxBookings,
}: PlanUsageWidgetProps) {
  const spacesPercentage = maxSpaces > 0 ? (currentSpaces / maxSpaces) * 100 : 0
  const bookingsPercentage = maxBookings > 0 ? (currentBookings / maxBookings) * 100 : 0
  
  const getBarColor = (percentage: number): string => {
    if (percentage >= 90) return '#dc3545' // Red for high usage
    if (percentage >= 75) return '#ffc107' // Yellow for medium usage
    return '#28a745' // Green for normal usage
  }
  
  return (
    <div style={styles.container}>
      <h3 style={styles.title}>Plan Usage</h3>
      
      {/* Spaces Usage */}
      <div style={styles.usageItem}>
        <div style={styles.labelRow}>
          <span style={styles.label}>Spaces</span>
          <span style={styles.value}>
            {currentSpaces} / {maxSpaces === 999999 ? '∞' : maxSpaces}
          </span>
        </div>
        <div style={styles.barContainer}>
          <div
            style={{
              ...styles.bar,
              width: `${Math.min(spacesPercentage, 100)}%`,
              backgroundColor: getBarColor(spacesPercentage),
            }}
          />
        </div>
      </div>
      
      {/* Bookings Usage */}
      <div style={styles.usageItem}>
        <div style={styles.labelRow}>
          <span style={styles.label}>Bookings this month</span>
          <span style={styles.value}>
            {currentBookings} / {maxBookings === 999999 ? '∞' : maxBookings}
          </span>
        </div>
        <div style={styles.barContainer}>
          <div
            style={{
              ...styles.bar,
              width: `${Math.min(bookingsPercentage, 100)}%`,
              backgroundColor: getBarColor(bookingsPercentage),
            }}
          />
        </div>
      </div>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    backgroundColor: 'white',
    padding: '24px',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
  },
  title: {
    fontSize: '16px',
    fontWeight: '600',
    marginBottom: '20px',
    color: '#333',
  },
  usageItem: {
    marginBottom: '20px',
  },
  labelRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: '8px',
  },
  label: {
    fontSize: '14px',
    color: '#666',
  },
  value: {
    fontSize: '14px',
    fontWeight: '500',
    color: '#333',
  },
  barContainer: {
    height: '8px',
    backgroundColor: '#e9ecef',
    borderRadius: '4px',
    overflow: 'hidden',
  },
  bar: {
    height: '100%',
    borderRadius: '4px',
    transition: 'width 0.3s ease, background-color 0.3s ease',
  },
}