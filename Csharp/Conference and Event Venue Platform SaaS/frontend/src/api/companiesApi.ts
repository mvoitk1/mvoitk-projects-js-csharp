// Companies API - Functions to manage companies (venues)
import { api } from './apiClient'
import { CreateCompanyRequest, CreateCompanyResponse } from '../types/apiTypes'

// POST /companies - Create a new company (become a venue owner)
export async function createCompany(payload: CreateCompanyRequest): Promise<CreateCompanyResponse> {
  return api.post<CreateCompanyResponse>('/companies', payload)
}
