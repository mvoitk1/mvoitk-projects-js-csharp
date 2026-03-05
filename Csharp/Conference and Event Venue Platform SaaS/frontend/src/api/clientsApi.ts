import { api } from './apiClient'
import type { Client, CreateClientRequest, UpdateClientRequest } from '../types/apiTypes'

export async function getClients(companySlug: string): Promise<Client[]> {
  const response = await api.get<Array<{
    id: string
    name: string
    companyId: string
    email?: string
    notes?: string
    createdUtc?: string
  }>>(`/${companySlug}/clients`)

  return response.map(c => ({
    ...c,
  }))
}

export async function getClient(companySlug: string, id: string): Promise<Client> {
  const response = await api.get<{
    id: string
    name: string
    email?: string
    notes?: string
    companyId: string
    createdUtc?: string
  }>(`/${companySlug}/clients/${id}`)

  return response
}

export async function createClient(companySlug: string, data: CreateClientRequest): Promise<Client> {
  const response = await api.post<{
    id: string
    name: string
    email?: string
    notes?: string
    companyId: string
    createdUtc?: string
  }>(`/${companySlug}/clients`, data)

  return response
}

export async function updateClient(companySlug: string, id: string, data: UpdateClientRequest): Promise<Client> {
  const response = await api.put<{
    id: string
    name: string
    email?: string
    notes?: string
    companyId: string
    createdUtc?: string
  }>(`/${companySlug}/clients/${id}`, data)

  return response
}

export async function deleteClient(companySlug: string, id: string): Promise<void> {
  await api.delete<void>(`/${companySlug}/clients/${id}`)
}
