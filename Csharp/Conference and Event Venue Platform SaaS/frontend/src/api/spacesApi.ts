import { api } from './apiClient'
import type { Space, CreateSpaceRequest, UpdateSpaceRequest, CreateEntityResponse } from '../types/apiTypes'

export async function getSpaces(companySlug: string): Promise<Space[]> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.get<Array<{
    id: string
    name: string
    capacity: number
    notes: string | null
    isActive: boolean
  }>>(`/${encodedCompanySlug}/spaces`)

  return response.map(s => ({
    ...s,
  }))
}

export async function getSpace(companySlug: string, id: string): Promise<Space> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.get<{
    id: string
    name: string
    capacity: number
    notes: string | null
    isActive: boolean
  }>(`/${encodedCompanySlug}/spaces/${id}`)

  return response
}

export async function createSpace(
  companySlug: string,
  payload: CreateSpaceRequest
): Promise<CreateEntityResponse> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.post<CreateEntityResponse>(`/${encodedCompanySlug}/spaces`, payload)

  return response
}

export async function updateSpace(
  companySlug: string,
  id: string,
  payload: UpdateSpaceRequest
): Promise<Space> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.put<{
    id: string
    name: string
    capacity: number
    notes: string | null
    isActive: boolean
  }>(`/${encodedCompanySlug}/spaces/${id}`, payload)

  return response
}

export async function deactivateSpace(
  companySlug: string,
  id: string
): Promise<{ id: string; name: string; isActive: boolean }> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.post<{
    id: string
    name: string
    isActive: boolean
  }>(`/${encodedCompanySlug}/spaces/${id}/deactivate`, {})

  return response
}
