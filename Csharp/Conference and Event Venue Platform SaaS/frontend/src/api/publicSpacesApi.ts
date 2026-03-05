// Public Spaces API - Functions for customer-facing space listings (no auth required)
import { apiRequest } from './apiClient'
import { PublicSpaceDto } from '../types/apiTypes'

// GET /companies/{companySlug}/spaces/public - Get public spaces for a venue
export async function getPublicSpaces(companySlug: string): Promise<PublicSpaceDto[]> {
  // skipAuth: true means this endpoint doesn't require a login token
  return apiRequest<PublicSpaceDto[]>(`/companies/${companySlug}/spaces/public`, { method: 'GET', skipAuth: true })
}
