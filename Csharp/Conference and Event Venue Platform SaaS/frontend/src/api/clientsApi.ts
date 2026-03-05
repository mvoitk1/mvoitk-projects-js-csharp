import { api } from './apiClient'
import { Client } from '../types/apiTypes'

export async function getClients(companySlug: string): Promise<Client[]> {
  const response = await api.get<Array<{
    id: string
    name: string
    companyId: string
  }>>(`/${companySlug}/clients`)

  // Backend list endpoint currently only returns id, name, companyId
  // Email and notes may be missing from list endpoint
  return response.map(c => ({
    ...c,
    email: undefined,
    notes: undefined,
  }))
}
