import { api } from './apiClient'
import { CreateCompanyRequest, CreateCompanyResponse } from '../types/apiTypes'

/**
 * Create a new company (become a venue owner)
 * Calls POST /companies (global endpoint, requires auth)
 */
export async function createCompany(payload: CreateCompanyRequest): Promise<CreateCompanyResponse> {
  return api.post<CreateCompanyResponse>('/companies', payload)
}
