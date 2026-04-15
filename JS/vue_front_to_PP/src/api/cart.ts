import { apiFetch } from './client'
import type { CartDto, AddToCartDto, UpdateCartItemDto } from '@/types'

export function getCart(): Promise<CartDto> {
  return apiFetch<CartDto>('/Cart')
}

export function addToCart(payload: AddToCartDto): Promise<CartDto> {
  return apiFetch<CartDto>('/Cart/items', 'POST', payload)
}

export function updateCartItem(cartItemId: string, payload: UpdateCartItemDto): Promise<CartDto> {
  return apiFetch<CartDto>(`/Cart/items/${cartItemId}`, 'PUT', payload)
}

export function removeCartItem(cartItemId: string): Promise<void> {
  return apiFetch<void>(`/Cart/items/${cartItemId}`, 'DELETE')
}
