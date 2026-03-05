import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ProtectedRoute } from './ProtectedRoute'
import { LoginPage } from '../pages/LoginPage'
import { DashboardPage } from '../pages/DashboardPage'
import { SpacesPage } from '../pages/SpacesPage'
import { ClientsPage } from '../pages/ClientsPage'
import { BookingsPage } from '../pages/BookingsPage'
import { CreateBookingPage } from '../pages/CreateBookingPage'
import { BookingDetailsPage } from '../pages/BookingDetailsPage'
import { DevBootstrapPage } from '../pages/DevBootstrapPage'

function RootRedirect() {
  const { isAuthenticated, companySlug } = useAuth()
  
  if (isAuthenticated && companySlug) {
    return <Navigate to={`/${companySlug}/dashboard`} replace />
  }
  
  return <Navigate to="/login" replace />
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public routes */}
        <Route path="/login" element={<LoginPage />} />
        
        {/* Dev-only routes */}
        {import.meta.env.DEV && (
          <Route path="/dev/bootstrap" element={<DevBootstrapPage />} />
        )}
        
        {/* Protected routes */}
        <Route
          path="/:companySlug/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/spaces"
          element={
            <ProtectedRoute>
              <SpacesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/clients"
          element={
            <ProtectedRoute>
              <ClientsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/bookings"
          element={
            <ProtectedRoute>
              <BookingsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/bookings/new"
          element={
            <ProtectedRoute>
              <CreateBookingPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/bookings/:id"
          element={
            <ProtectedRoute>
              <BookingDetailsPage />
            </ProtectedRoute>
          }
        />
        
        {/* Root redirect */}
        <Route path="/" element={<RootRedirect />} />
        
        {/* Catch all - redirect to root */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}