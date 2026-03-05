import { api } from './apiClient'
import type {
  CreateBookingRequestRequest,
  CreateBookingRequestResponse,
} from '../types/apiTypes'

export async function createBookingRequest(
  companySlug: string,
  payload: CreateBookingRequestRequest
): Promise<CreateBookingRequestResponse> {
  return api.post<CreateBookingRequestResponse>(
    `/companies/${companySlug}/booking-requests`,
    payload
  )
}
