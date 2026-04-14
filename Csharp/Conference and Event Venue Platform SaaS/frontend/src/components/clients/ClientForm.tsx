import { useState, useEffect } from 'react'
import type { Client, CreateClientRequest, UpdateClientRequest } from '../../types/apiTypes'

interface ClientFormProps {
  initialValue?: Client | null
  onSubmit: (data: CreateClientRequest | UpdateClientRequest) => void
  isSubmitting: boolean
  submitLabel?: string
}

export function ClientForm({ initialValue, onSubmit, isSubmitting, submitLabel = 'Save' }: ClientFormProps) {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [notes, setNotes] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  // Load initial values when editing
  useEffect(() => {
    if (initialValue) {
      setName(initialValue.name || '')
      setEmail(initialValue.email || '')
      setNotes(initialValue.notes || '')
    }
  }, [initialValue])

  const validateEmail = (email: string): boolean => {
    if (!email) return true // Email is optional
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    return emailRegex.test(email)
  }

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!name.trim()) {
      newErrors.name = 'Name is required'
    }

    if (email && !validateEmail(email)) {
      newErrors.email = 'Please enter a valid email address'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) {
      return
    }

    const data: CreateClientRequest | UpdateClientRequest = {
      name: name.trim(),
      email: email.trim() || undefined,
      notes: notes.trim() || undefined,
    }

    onSubmit(data)
  }

  return (
    <form onSubmit={handleSubmit} style={styles.form}>
      <div style={styles.formGroup}>
        <label htmlFor="name" style={styles.label}>
          Name <span style={styles.required}>*</span>
        </label>
        <input
          id="name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Enter client name"
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
        <label htmlFor="email" style={styles.label}>
          Email
        </label>
        <input
          id="email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Enter email address (optional)"
          style={{
            ...styles.input,
            ...(errors.email ? styles.inputError : {}),
          }}
          maxLength={255}
          disabled={isSubmitting}
        />
        {errors.email && <span style={styles.errorText}>{errors.email}</span>}
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
          rows={4}
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
    minHeight: '100px',
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
