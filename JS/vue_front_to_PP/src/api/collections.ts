import { apiFetch } from './client'
import type { CollectionDto } from '@/types'

export function getCollections(): Promise<CollectionDto[]> {
  return apiFetch<CollectionDto[]>('/Collections')
}
