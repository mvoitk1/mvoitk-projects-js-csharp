import { apiFetch } from './client'
import type { CategoryDto } from '@/types'

export function getCategories(): Promise<CategoryDto[]> {
  return apiFetch<CategoryDto[]>('/Categories')
}
