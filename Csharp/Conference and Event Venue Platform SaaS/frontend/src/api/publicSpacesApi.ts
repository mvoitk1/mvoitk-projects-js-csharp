import { apiRequest } from './apiClient'
import { PublicSpaceDto } from '../types/apiTypes'

/**
 * Get list of public spaces for a specific venue (company)
 * Calls GET /companies/{companySlug}/spaces/public (global endpoint, PUBLIC - no auth required)
 */
export async function getPublicSpaces(companySlug: string): Promise<PublicSpaceDto[]> {
  return apiRequest<PublicSpaceDto[]>(`/companies/${companySlug}/spaces/public`, { method: 'GET', skipAuth: true })
}
