import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { bootstrapTenant, type BootstrapRequest } from '../api/devApi';
import { useAuth } from '../auth/useAuth';
import { normalizeApiError } from '../api/apiClient';

interface BootstrapResult {
  companyId: string;
  userId: string;
  companySlug: string;
  role: string;
}

const defaultFormData: BootstrapRequest = {
  companyName: 'Acme Co',
  companySlug: 'acme',
  email: 'owner@acme.com',
  password: 'Pass123$',
  role: 'CompanyOwner',
};

const availableRoles = ['CompanyOwner', 'Admin', 'User'];

/**
 * Converts an unknown value to a safe display string for rendering.
 */
function toDisplayString(value: unknown): string {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

export function DevBootstrapPage(): JSX.Element {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [formData, setFormData] = useState<BootstrapRequest>(defaultFormData);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<BootstrapResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showDetails, setShowDetails] = useState(false);
  const [errorDetails, setErrorDetails] = useState<unknown>(null);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setResult(null);
    setErrorDetails(null);

    try {
      const response = await bootstrapTenant(formData);
      setResult(response);
    } catch (err: unknown) {
      const normalized = normalizeApiError(err);
      setError(normalized.message);
      setErrorDetails(normalized);
    } finally {
      setLoading(false);
    }
  };

  const handleLoginAsOwner = async () => {
    if (!result) return;

    setLoading(true);
    setError(null);

    try {
      // AuthContext.login expects (credentials, companySlug)
      await login(
        { email: formData.email, password: formData.password },
        result.companySlug
      );
      navigate(`/${result.companySlug}/dashboard`);
    } catch (err: unknown) {
      const normalized = normalizeApiError(err);
      setError(normalized.message);
      setErrorDetails(normalized);
    } finally {
      setLoading(false);
    }
  };

  const isDev = import.meta.env.DEV;

  // Safety check: this page should only render in dev mode
  if (!isDev) {
    return (
      <div style={{ 
        maxWidth: '500px', 
        margin: '2rem auto', 
        padding: '1rem',
        textAlign: 'center'
      }}>
        <h1>Not Available</h1>
        <p>This page is only available in development mode.</p>
        <Link to="/login" style={{ color: '#2563eb' }}>
          ← Back to Login
        </Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '500px', margin: '2rem auto', padding: '1rem' }}>
      <h1>Dev Bootstrap</h1>
      <p style={{ color: '#666' }}>
        Development-only: Create a company, user, and membership in one step.
      </p>

      {error && (
        <div
          style={{
            padding: '0.75rem',
            backgroundColor: '#fee2e2',
            color: '#dc2626',
            borderRadius: '4px',
            marginBottom: '1rem',
          }}
        >
          {error}
          
          {/* Dev-only error details */}
          {isDev && errorDetails !== null && (
            <div style={{ marginTop: '8px' }}>
              <button
                type="button"
                onClick={() => setShowDetails(!showDetails)}
                style={{
                  fontSize: '12px',
                  color: '#dc2626',
                  textDecoration: 'underline',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  padding: 0,
                }}
              >
                {showDetails ? 'Hide Details' : 'Show Details'}
              </button>
              
              {showDetails && (
                <pre style={{ 
                  marginTop: '8px',
                  padding: '8px',
                  background: '#fef2f2',
                  borderRadius: '4px',
                  fontSize: '11px',
                  overflow: 'auto',
                  maxHeight: '200px'
                }}>
                  {toDisplayString(errorDetails)}
                </pre>
              )}
            </div>
          )}
        </div>
      )}

      {!result ? (
        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
              Company Name
            </label>
            <input
              type="text"
              name="companyName"
              value={formData.companyName}
              onChange={handleChange}
              style={{
                width: '100%',
                padding: '0.5rem',
                borderRadius: '4px',
                border: '1px solid #ccc',
                fontSize: '14px',
                boxSizing: 'border-box',
              }}
              required
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
              Company Slug
            </label>
            <input
              type="text"
              name="companySlug"
              value={formData.companySlug}
              onChange={handleChange}
              style={{
                width: '100%',
                padding: '0.5rem',
                borderRadius: '4px',
                border: '1px solid #ccc',
                fontSize: '14px',
                boxSizing: 'border-box',
              }}
              required
              pattern="[a-z0-9-]+"
              title="Lowercase letters, numbers, and hyphens only"
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
              Email
            </label>
            <input
              type="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              style={{
                width: '100%',
                padding: '0.5rem',
                borderRadius: '4px',
                border: '1px solid #ccc',
                fontSize: '14px',
                boxSizing: 'border-box',
              }}
              required
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
              Password
            </label>
            <input
              type="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              style={{
                width: '100%',
                padding: '0.5rem',
                borderRadius: '4px',
                border: '1px solid #ccc',
                fontSize: '14px',
                boxSizing: 'border-box',
              }}
              required
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
              Role
            </label>
            <select
              name="role"
              value={formData.role}
              onChange={handleChange}
              style={{
                width: '100%',
                padding: '0.5rem',
                borderRadius: '4px',
                border: '1px solid #ccc',
                fontSize: '14px',
                boxSizing: 'border-box',
              }}
            >
              {availableRoles.map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
          </div>

          <button
            type="submit"
            disabled={loading}
            style={{
              width: '100%',
              padding: '0.75rem',
              backgroundColor: '#2563eb',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: loading ? 'not-allowed' : 'pointer',
              opacity: loading ? 0.7 : 1,
              fontSize: '14px',
              fontWeight: 500,
            }}
          >
            {loading ? 'Creating...' : 'Create Company + Owner'}
          </button>
        </form>
      ) : (
        <div>
          <div
            style={{
              padding: '1rem',
              backgroundColor: '#d1fae5',
              color: '#065f46',
              borderRadius: '4px',
              marginBottom: '1rem',
            }}
          >
            <h3 style={{ marginTop: 0, marginBottom: '0.75rem' }}>Success!</h3>
            <p style={{ margin: '0.25rem 0', fontSize: '14px' }}>
              <strong>Company ID:</strong> {result.companyId}
            </p>
            <p style={{ margin: '0.25rem 0', fontSize: '14px' }}>
              <strong>User ID:</strong> {result.userId}
            </p>
            <p style={{ margin: '0.25rem 0', fontSize: '14px' }}>
              <strong>Company Slug:</strong> {result.companySlug}
            </p>
            <p style={{ margin: '0.25rem 0', fontSize: '14px' }}>
              <strong>Role:</strong> {result.role}
            </p>
          </div>

          <button
            onClick={handleLoginAsOwner}
            disabled={loading}
            style={{
              width: '100%',
              padding: '0.75rem',
              backgroundColor: '#059669',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: loading ? 'not-allowed' : 'pointer',
              opacity: loading ? 0.7 : 1,
              fontSize: '14px',
              fontWeight: 500,
            }}
          >
            {loading ? 'Logging in...' : 'Login as Owner'}
          </button>

          <button
            onClick={() => {
              setResult(null);
              setError(null);
              setErrorDetails(null);
            }}
            style={{
              width: '100%',
              padding: '0.75rem',
              marginTop: '0.5rem',
              backgroundColor: '#6b7280',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: 500,
            }}
          >
            Create Another
          </button>
        </div>
      )}

      <div style={{ marginTop: '1.5rem' }}>
        <Link to="/login" style={{ color: '#2563eb', fontSize: '14px' }}>
          ← Back to Login
        </Link>
      </div>
    </div>
  );
}
