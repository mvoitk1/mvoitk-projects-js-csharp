import { apiFetch } from './client'
import type {
  AdminCategoryDto,
  AdminCategoryWriteDto,
  AdminCollectionDto,
  AdminCollectionWriteDto,
  AdminProductDto,
  AdminProductWriteDto,
  AdminVariantDto,
  AdminVariantWriteDto,
  AdminProductImageDto,
  AdminProductImageWriteDto,
  AdminOrderDto,
  AdminOrderStatusDto,
  AdminStockUpdateDto,
} from '@/types'

// ─── Categories ──────────────────────────────────────────────────────────────

export function adminGetCategories(): Promise<AdminCategoryDto[]> {
  return apiFetch<AdminCategoryDto[]>('/admin/categories')
}

export function adminGetCategory(id: string): Promise<AdminCategoryDto> {
  return apiFetch<AdminCategoryDto>(`/admin/categories/${id}`)
}

export function adminCreateCategory(payload: AdminCategoryWriteDto): Promise<AdminCategoryDto> {
  return apiFetch<AdminCategoryDto>('/admin/categories', 'POST', payload)
}

export function adminUpdateCategory(
  id: string,
  payload: AdminCategoryWriteDto,
): Promise<AdminCategoryDto> {
  return apiFetch<AdminCategoryDto>(`/admin/categories/${id}`, 'PUT', payload)
}

export function adminDeleteCategory(id: string): Promise<void> {
  return apiFetch<void>(`/admin/categories/${id}`, 'DELETE')
}

// ─── Collections ─────────────────────────────────────────────────────────────

export function adminGetCollections(): Promise<AdminCollectionDto[]> {
  return apiFetch<AdminCollectionDto[]>('/admin/collections')
}

export function adminGetCollection(id: string): Promise<AdminCollectionDto> {
  return apiFetch<AdminCollectionDto>(`/admin/collections/${id}`)
}

export function adminCreateCollection(
  payload: AdminCollectionWriteDto,
): Promise<AdminCollectionDto> {
  return apiFetch<AdminCollectionDto>('/admin/collections', 'POST', payload)
}

export function adminUpdateCollection(
  id: string,
  payload: AdminCollectionWriteDto,
): Promise<AdminCollectionDto> {
  return apiFetch<AdminCollectionDto>(`/admin/collections/${id}`, 'PUT', payload)
}

export function adminDeleteCollection(id: string): Promise<void> {
  return apiFetch<void>(`/admin/collections/${id}`, 'DELETE')
}

// ─── Products ────────────────────────────────────────────────────────────────

export function adminGetProducts(): Promise<AdminProductDto[]> {
  return apiFetch<AdminProductDto[]>('/admin/products')
}

export function adminGetProduct(id: string): Promise<AdminProductDto> {
  return apiFetch<AdminProductDto>(`/admin/products/${id}`)
}

export function adminCreateProduct(payload: AdminProductWriteDto): Promise<AdminProductDto> {
  return apiFetch<AdminProductDto>('/admin/products', 'POST', payload)
}

export function adminUpdateProduct(
  id: string,
  payload: AdminProductWriteDto,
): Promise<AdminProductDto> {
  return apiFetch<AdminProductDto>(`/admin/products/${id}`, 'PUT', payload)
}

export function adminDeleteProduct(id: string): Promise<void> {
  return apiFetch<void>(`/admin/products/${id}`, 'DELETE')
}

// ─── Variants ────────────────────────────────────────────────────────────────

export function adminAddVariant(
  productId: string,
  payload: AdminVariantWriteDto,
): Promise<AdminVariantDto> {
  return apiFetch<AdminVariantDto>(`/admin/products/${productId}/variants`, 'POST', payload)
}

export function adminUpdateVariant(
  productId: string,
  variantId: string,
  payload: AdminVariantWriteDto,
): Promise<AdminVariantDto> {
  return apiFetch<AdminVariantDto>(
    `/admin/products/${productId}/variants/${variantId}`,
    'PUT',
    payload,
  )
}

export function adminDeleteVariant(productId: string, variantId: string): Promise<void> {
  return apiFetch<void>(`/admin/products/${productId}/variants/${variantId}`, 'DELETE')
}

// ─── Images ──────────────────────────────────────────────────────────────────

export function adminAddImage(
  productId: string,
  payload: AdminProductImageWriteDto,
): Promise<AdminProductImageDto> {
  return apiFetch<AdminProductImageDto>(`/admin/products/${productId}/images`, 'POST', payload)
}

export function adminDeleteImage(productId: string, imageId: string): Promise<void> {
  return apiFetch<void>(`/admin/products/${productId}/images/${imageId}`, 'DELETE')
}

// ─── Stock ───────────────────────────────────────────────────────────────────

export function adminUpdateStock(
  variantId: string,
  payload: AdminStockUpdateDto,
): Promise<void> {
  return apiFetch<void>(`/admin/stock/${variantId}`, 'PUT', payload)
}

// ─── Orders ──────────────────────────────────────────────────────────────────

export function adminGetOrders(status?: string): Promise<AdminOrderDto[]> {
  const qs = status ? `?status=${encodeURIComponent(status)}` : ''
  return apiFetch<AdminOrderDto[]>(`/admin/orders${qs}`)
}

export function adminGetOrder(id: string): Promise<AdminOrderDto> {
  return apiFetch<AdminOrderDto>(`/admin/orders/${id}`)
}

export function adminUpdateOrderStatus(
  id: string,
  payload: AdminOrderStatusDto,
): Promise<void> {
  return apiFetch<void>(`/admin/orders/${id}/status`, 'PUT', payload)
}
