import { apiFetch } from './client'
import type { OrderListItemDto, OrderDto, CreateOrderDto } from '@/types'

export function getOrders(): Promise<OrderListItemDto[]> {
  return apiFetch<OrderListItemDto[]>('/Orders')
}

export function getOrder(id: string): Promise<OrderDto> {
  return apiFetch<OrderDto>(`/Orders/${id}`)
}

export function createOrder(payload: CreateOrderDto): Promise<OrderDto> {
  return apiFetch<OrderDto>('/Orders', 'POST', payload)
}
