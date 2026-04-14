// Public Companies API - Functions for customer-facing venue directory (no auth required)
import { apiRequest } from './apiClient'
import { PublicCompaniesResponse } from '../types/apiTypes'

// GET /companies/public - Get list of public venues for customer directory
export async function getPublicCompanies(): Promise<PublicCompaniesResponse> {
  // skipAuth: true means this endpoint doesn't require a login token
  return apiRequest<PublicCompaniesResponse>('/companies/public', { method: 'GET', skipAuth: true })
}
