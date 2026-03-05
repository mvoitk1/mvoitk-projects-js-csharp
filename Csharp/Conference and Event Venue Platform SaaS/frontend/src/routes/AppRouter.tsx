import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ProtectedRoute } from './ProtectedRoute'
import { SessionGate } from '../auth/SessionGate'
import { AppLayout } from '../layout/AppLayout'
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
import { SelectCompanyPage } from '../pages/SelectCompanyPage'
import { CustomerHomePage } from '../pages/CustomerHomePage'
import { BecomeVenuePage } from '../pages/BecomeVenuePage'
import { CustomerVenuesPage } from '../pages/CustomerVenuesPage'
import { CustomerVenueDetailsPage } from '../pages/CustomerVenueDetailsPage'

function RootRedirect() {
  const { isAuthenticated } = useAuth()
  
  // If not authenticated, go to login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }
  
  // Authenticated users hitting root should go through SessionGate
  // to determine correct routing based on company membership
  return <Navigate to="/session" replace />
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
        
        {/* Global protected routes (no company slug required) */}
        <Route
          path="/session"
          element={
            <ProtectedRoute>
              <SessionGate />
            </ProtectedRoute>
          }
        />
        <Route
          path="/select-company"
          element={
            <ProtectedRoute>
              <AppLayout mode="customer">
                <SelectCompanyPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/customer"
          element={
            <ProtectedRoute>
              <AppLayout mode="customer">
                <CustomerHomePage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/become-a-venue"
          element={
            <ProtectedRoute>
              <AppLayout mode="customer">
                <BecomeVenuePage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/customer/venues"
          element={
            <ProtectedRoute>
              <AppLayout mode="customer">
                <CustomerVenuesPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/customer/venues/:companySlug"
          element={
            <ProtectedRoute>
              <AppLayout mode="customer">
                <CustomerVenueDetailsPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />

        {/* Protected tenant routes (require company slug) */}
        <Route
          path="/:companySlug/dashboard"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <DashboardPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/spaces"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <SpacesPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/spaces/new"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <CreateSpacePage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/spaces/:id"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <SpaceDetailsPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/spaces/:id/edit"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <EditSpacePage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/clients"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <ClientsPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/clients/new"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <CreateClientPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/clients/:id"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <ClientDetailsPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/clients/:id/edit"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <EditClientPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/bookings"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <BookingsPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/bookings/new"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <CreateBookingPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/bookings/:id"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <BookingDetailsPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/invoices"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <InvoicesPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/invoices/:id"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <InvoiceDetailsPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/billing"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <BillingPlanPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/reports/revenue"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <RevenueReportPage />
              </AppLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/:companySlug/reports/occupancy"
          element={
            <ProtectedRoute>
              <AppLayout mode="tenant">
                <OccupancyReportPage />
              </AppLayout>
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
