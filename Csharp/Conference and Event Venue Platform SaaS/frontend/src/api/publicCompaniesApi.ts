import { apiRequest } from './apiClient'
import { PublicCompaniesResponse } from '../types/apiTypes'

/**
 * Get list of public companies (venues) for customer directory
 * Calls GET /companies/public (global endpoint, PUBLIC - no auth required)
 */
export async function getPublicCompanies(): Promise<PublicCompaniesResponse> {
  return apiRequest<PublicCompaniesResponse>('/companies/public', { method: 'GET', skipAuth: true })
}
