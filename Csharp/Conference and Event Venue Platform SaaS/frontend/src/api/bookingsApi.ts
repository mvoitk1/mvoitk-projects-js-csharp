import { api } from './apiClient'
import type { Booking, BookingDetails, CreateBookingRequest } from '../types/apiTypes'

export async function getBookings(companySlug: string): Promise<Booking[]> {
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
  }>>(`/${companySlug}/bookings`)

  return response.map(b => ({
    ...b,
    status: b.status,
  }))
}

export async function getBookingDetails(companySlug: string, id: string): Promise<BookingDetails> {
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
  }>(`/${companySlug}/bookings/${id}/details`)

  return {
    ...response,
    spaces: response.spaces || [],
  }
}

export async function createBookingWithSpaces(
  companySlug: string,
  payload: CreateBookingRequest
): Promise<Booking> {
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
  }>(`/${companySlug}/bookings/with-spaces`, payload)

  return response
}

export async function confirmBooking(companySlug: string, id: string): Promise<void> {
  await api.post<void>(`/${companySlug}/bookings/${id}/confirm`, {})
}

export async function cancelBooking(companySlug: string, id: string, reason?: string): Promise<void> {
  const queryParams = reason ? `?reason=${encodeURIComponent(reason)}` : ''
  await api.delete<void>(`/${companySlug}/bookings/${id}${queryParams}`)
}
