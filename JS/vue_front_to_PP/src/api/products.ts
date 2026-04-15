import { apiFetch } from './client'
import type { ProductListItemDto, ProductDto } from '@/types'

export interface ProductFilters {
  categoryId?: string
  collectionId?: string
  gender?: string
}

export function getProducts(filters: ProductFilters = {}): Promise<ProductListItemDto[]> {
  const params = new URLSearchParams()
  if (filters.categoryId) params.set('categoryId', filters.categoryId)
  if (filters.collectionId) params.set('collectionId', filters.collectionId)
  if (filters.gender) params.set('gender', filters.gender)
  const qs = params.toString()
  return apiFetch<ProductListItemDto[]>(`/Products${qs ? `?${qs}` : ''}`)
}

export function getProduct(id: string): Promise<ProductDto> {
  return apiFetch<ProductDto>(`/Products/${id}`)
}
