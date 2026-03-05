import { api } from './apiClient'
import { Space, CreateSpaceRequest } from '../types/apiTypes'

export async function getSpaces(companySlug: string): Promise<Space[]> {
  const response = await api.get<Array<{
    id: string
    name: string
    capacity: number
    hourlyRate: number
    companyId: string
  }>>(`/${companySlug}/spaces`)

  // Map response to include isActive (backend list doesn't include it, default to true)
  return response.map(s => ({
    ...s,
    isActive: true,
  }))
}

export async function getSpace(companySlug: string, id: string): Promise<Space> {
  const response = await api.get<{
    id: string
    name: string
    capacity: number
    hourlyRate: number
    isActive: boolean
    companyId: string
  }>(`/${companySlug}/spaces/${id}`)

  return response
}

export async function createSpace(
  companySlug: string,
  payload: CreateSpaceRequest
): Promise<Space> {
  const response = await api.post<{
    id: string
    name: string
    capacity: number
    hourlyRate: number
  }>(`/${companySlug}/spaces`, payload)

  return {
    ...response,
    isActive: true,
    companyId: '', // Will be filled in by caller or refetch
  }
}

export async function deactivateSpace(
  companySlug: string,
  id: string
): Promise<{ id: string; name: string; isActive: boolean }> {
  const response = await api.post<{
    id: string
    name: string
    isActive: boolean
  }>(`/${companySlug}/spaces/${id}/deactivate`, {})

  return response
}
