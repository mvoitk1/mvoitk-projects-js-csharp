// Bookings API - Functions to manage bookings (reservations of spaces)
import { api } from './apiClient'
import type { Booking, BookingDetails, CreateBookingRequest } from '../types/apiTypes'

// GET /{companySlug}/bookings - Fetch all bookings for a company
export async function getBookings(companySlug: string): Promise<Booking[]> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.get<Array<{
    id: string
    clientId: string
    title: string
    startUtc: string
    endUtc: string
    attendeeCount: number
    isCancelled: boolean
    spaceIds: string[]
    totalAmount: number
    spaceConfigurationId?: string
    status: 'Pending' | 'Confirmed'
    cancelledUtc?: string
    cancelReason?: string
    createdByUserId?: string
    confirmedByUserId?: string
    cancelledByUserId?: string
  }>>(`/${encodedCompanySlug}/bookings`)

  return response.map(b => ({
    ...b,
    status: b.status,
  }))
}

// GET /{companySlug}/bookings/{id}/details - Fetch single booking with full details
export async function getBookingDetails(companySlug: string, id: string): Promise<BookingDetails> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.get<{
    id: string
    clientId: string
    clientName: string
    title: string
    startUtc: string
    endUtc: string
    attendeeCount: number
    isCancelled: boolean
    totalAmount: number
    spaceConfigurationId?: string
    spaces: Array<{ id: string; name: string }>
    status: 'Pending' | 'Confirmed'
    cancelledUtc?: string
    cancelReason?: string
    createdByUserId?: string
    confirmedByUserId?: string
    cancelledByUserId?: string
  }>(`/${encodedCompanySlug}/bookings/${id}/details`)

  return {
    ...response,
    spaces: response.spaces || [],
  }
}

// POST /{companySlug}/bookings/with-spaces - Create new booking with spaces
export async function createBookingWithSpaces(
  companySlug: string,
  payload: CreateBookingRequest
): Promise<Booking> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.post<{
    id: string
    clientId: string
    title: string
    startUtc: string
    endUtc: string
    attendeeCount: number
    isCancelled: boolean
    spaceIds: string[]
    totalAmount: number
    spaceConfigurationId?: string
    status: 'Pending' | 'Confirmed'
    cancelledUtc?: string
    cancelReason?: string
    createdByUserId?: string
    confirmedByUserId?: string
    cancelledByUserId?: string
  }>(`/${encodedCompanySlug}/bookings/with-spaces`, payload)

  return response
}

// POST /{companySlug}/bookings/{id}/confirm - Confirm a pending booking
export async function confirmBooking(companySlug: string, id: string): Promise<void> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  await api.post<void>(`/${encodedCompanySlug}/bookings/${id}/confirm`, {})
}

// DELETE /{companySlug}/bookings/{id} - Cancel a booking
export async function cancelBooking(companySlug: string, id: string, reason?: string): Promise<void> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const queryParams = reason ? `?reason=${encodeURIComponent(reason)}` : ''
  await api.delete<void>(`/${encodedCompanySlug}/bookings/${id}${queryParams}`)
}

// POST /{companySlug}/bookings/{bookingId}/invoice - Generate invoice from booking
export async function createInvoiceFromBooking(
  companySlug: string,
  bookingId: string
): Promise<string> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.post<{ invoiceId: string }>(
    `/${encodedCompanySlug}/bookings/${bookingId}/invoice`,
    {}
  )
  return response.invoiceId
}
