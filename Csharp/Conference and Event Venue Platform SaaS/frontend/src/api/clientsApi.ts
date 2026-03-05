import { api } from './apiClient'
import type { Client, CreateClientRequest, UpdateClientRequest, CreateEntityResponse } from '../types/apiTypes'

export async function getClients(companySlug: string): Promise<Client[]> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.get<Array<{
    id: string
    name: string
    companyId: string
    email?: string
    notes?: string
    createdUtc?: string
  }>>(`/${encodedCompanySlug}/clients`)

  return response.map(c => ({
    ...c,
  }))
}

export async function getClient(companySlug: string, id: string): Promise<Client> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.get<{
    id: string
    name: string
    email?: string
    notes?: string
    companyId: string
    createdUtc?: string
  }>(`/${encodedCompanySlug}/clients/${id}`)

  return response
}

export async function createClient(companySlug: string, data: CreateClientRequest): Promise<CreateEntityResponse> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.post<CreateEntityResponse>(`/${encodedCompanySlug}/clients`, data)

  return response
}

export async function updateClient(companySlug: string, id: string, data: UpdateClientRequest): Promise<Client> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.put<{
    id: string
    name: string
    email?: string
    notes?: string
    companyId: string
    createdUtc?: string
  }>(`/${encodedCompanySlug}/clients/${id}`, data)

  return response
}

export async function deleteClient(companySlug: string, id: string): Promise<void> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  await api.delete<void>(`/${encodedCompanySlug}/clients/${id}`)
}
