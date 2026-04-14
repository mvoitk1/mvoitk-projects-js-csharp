import { useState, useEffect } from 'react'

interface SpaceFormProps {
  initialValue?: {
    name: string
    capacity: number
    notes?: string
  }
  onSubmit: (data: { name: string; capacity: number; notes?: string }) => void
  isSubmitting?: boolean
  submitLabel: string
}

export function SpaceForm({
  initialValue,
  onSubmit,
  isSubmitting = false,
  submitLabel,
}: SpaceFormProps) {
  const [name, setName] = useState('')
  const [capacity, setCapacity] = useState<number | ''>('')
  const [notes, setNotes] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  // Load initial values when editing
  useEffect(() => {
    if (initialValue) {
      setName(initialValue.name || '')
      setCapacity(initialValue.capacity ?? '')
      setNotes(initialValue.notes || '')
    }
  }, [initialValue])

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!name.trim()) {
      newErrors.name = 'Space name is required'
    }

    if (capacity !== '' && (isNaN(Number(capacity)) || Number(capacity) < 0)) {
      newErrors.capacity = 'Capacity must be 0 or greater'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) {
      return
    }

    const data: { name: string; capacity: number; notes?: string } = {
      name: name.trim(),
      capacity: capacity === '' ? 0 : Number(capacity),
      notes: notes.trim() || undefined,
    }

    onSubmit(data)
  }

  return (
    <form onSubmit={handleSubmit} style={styles.form}>
      <div style={styles.formGroup}>
        <label htmlFor="name" style={styles.label}>
          Space Name <span style={styles.required}>*</span>
        </label>
        <input
          id="name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Enter space name"
          style={{
            ...styles.input,
            ...(errors.name ? styles.inputError : {}),
          }}
          maxLength={200}
          disabled={isSubmitting}
        />
        {errors.name && <span style={styles.errorText}>{errors.name}</span>}
      </div>

      <div style={styles.formGroup}>
        <label htmlFor="capacity" style={styles.label}>
          Capacity
        </label>
        <input
          id="capacity"
          type="number"
          value={capacity}
          onChange={(e) =>
            setCapacity(e.target.value === '' ? '' : Number(e.target.value))
          }
          placeholder="Enter capacity"
          style={{
            ...styles.input,
            ...(errors.capacity ? styles.inputError : {}),
          }}
          min={0}
          disabled={isSubmitting}
        />
        {errors.capacity && (
          <span style={styles.errorText}>{errors.capacity}</span>
        )}
      </div>

      <div style={styles.formGroup}>
        <label htmlFor="notes" style={styles.label}>
          Notes
        </label>
        <textarea
          id="notes"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Enter any additional notes (optional)"
          style={styles.textarea}
          rows={3}
          maxLength={2000}
          disabled={isSubmitting}
        />
      </div>

      <div style={styles.formActions}>
        <button
          type="submit"
          style={{
            ...styles.submitButton,
            ...(isSubmitting ? styles.submitButtonDisabled : {}),
          }}
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Saving...' : submitLabel}
        </button>
      </div>
    </form>
  )
}

const styles: Record<string, React.CSSProperties> = {
  form: {
    backgroundColor: 'white',
    padding: '24px',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
  },
  formGroup: {
    marginBottom: '20px',
  },
  label: {
    display: 'block',
    marginBottom: '8px',
    fontSize: '14px',
    fontWeight: '600',
    color: '#333',
  },
  required: {
    color: '#c33',
  },
  input: {
    width: '100%',
    padding: '10px 12px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    boxSizing: 'border-box',
  },
  inputError: {
    borderColor: '#c33',
  },
  errorText: {
    display: 'block',
    marginTop: '6px',
    fontSize: '13px',
    color: '#c33',
  },
  textarea: {
    width: '100%',
    padding: '10px 12px',
    fontSize: '14px',
    border: '1px solid #ddd',
    borderRadius: '4px',
    boxSizing: 'border-box',
    resize: 'vertical',
    fontFamily: 'inherit',
    minHeight: '80px',
  },
  formActions: {
    display: 'flex',
    justifyContent: 'flex-end',
    marginTop: '24px',
    paddingTop: '20px',
    borderTop: '1px solid #e0e0e0',
  },
  submitButton: {
    padding: '10px 24px',
    fontSize: '14px',
    fontWeight: '600',
    border: 'none',
    borderRadius: '4px',
    backgroundColor: '#007bff',
    color: 'white',
    cursor: 'pointer',
  },
  submitButtonDisabled: {
    opacity: 0.6,
    cursor: 'not-allowed',
  },
}
