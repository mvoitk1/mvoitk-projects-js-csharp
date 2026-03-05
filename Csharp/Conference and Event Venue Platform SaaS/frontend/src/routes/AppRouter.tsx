import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ProtectedRoute } from './ProtectedRoute'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'
import { DashboardPage } from '../pages/DashboardPage'
import { SpacesPage } from '../pages/SpacesPage'
import { CreateSpacePage } from '../pages/CreateSpacePage'
import { SpaceDetailsPage } from '../pages/SpaceDetailsPage'
import { EditSpacePage } from '../pages/EditSpacePage'
import { ClientsPage } from '../pages/ClientsPage'
import { CreateClientPage } from '../pages/CreateClientPage'
import { ClientDetailsPage } from '../pages/ClientDetailsPage'
import { EditClientPage } from '../pages/EditClientPage'
import { BookingsPage } from '../pages/BookingsPage'
import { CreateBookingPage } from '../pages/CreateBookingPage'
import { BookingDetailsPage } from '../pages/BookingDetailsPage'
import { InvoicesPage } from '../pages/InvoicesPage'
import { InvoiceDetailsPage } from '../pages/InvoiceDetailsPage'
import { BillingPlanPage } from '../pages/BillingPlanPage'
import { RevenueReportPage } from '../pages/RevenueReportPage'
import { OccupancyReportPage } from '../pages/OccupancyReportPage'
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
        <Route path="/register" element={<RegisterPage />} />

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
          path="/:companySlug/spaces/new"
          element={
            <ProtectedRoute>
              <CreateSpacePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/spaces/:id"
          element={
            <ProtectedRoute>
              <SpaceDetailsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/spaces/:id/edit"
          element={
            <ProtectedRoute>
              <EditSpacePage />
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
          path="/:companySlug/clients/new"
          element={
            <ProtectedRoute>
              <CreateClientPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/clients/:id"
          element={
            <ProtectedRoute>
              <ClientDetailsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/clients/:id/edit"
          element={
            <ProtectedRoute>
              <EditClientPage />
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
        <Route
          path="/:companySlug/invoices"
          element={
            <ProtectedRoute>
              <InvoicesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/invoices/:id"
          element={
            <ProtectedRoute>
              <InvoiceDetailsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/billing"
          element={
            <ProtectedRoute>
              <BillingPlanPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/reports/revenue"
          element={
            <ProtectedRoute>
              <RevenueReportPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/reports/occupancy"
          element={
            <ProtectedRoute>
              <OccupancyReportPage />
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