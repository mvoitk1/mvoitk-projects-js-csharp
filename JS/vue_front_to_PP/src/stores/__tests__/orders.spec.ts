import { describe, it, expect, vi } from 'vitest'
import { useOrdersStore } from '@/stores/orders'
import type { OrderDto, OrderListItemDto, CreateOrderDto } from '@/types'

vi.mock('@/api/orders', () => ({
  getOrders: vi.fn(),
  getOrder: vi.fn(),
  createOrder: vi.fn(),
}))

import * as ordersApi from '@/api/orders'

const mocked = vi.mocked(ordersApi)

const shipping: CreateOrderDto = {
  shippingFirstName: 'A',
  shippingLastName: 'B',
  shippingEmail: 'a@b.c',
  shippingPhone: '1',
  shippingCountry: 'EE',
  shippingCity: 'Tallinn',
  shippingStreet: 'Main 1',
  shippingPostalCode: '10000',
}

describe('orders store', () => {
  it('fetchOrders stores the list and resets loading', async () => {
    const list = [{ id: 'o1' }] as OrderListItemDto[]
    mocked.getOrders.mockResolvedValue(list)
    const store = useOrdersStore()

    await store.fetchOrders()

    expect(store.list).toEqual(list)
    expect(store.loading).toBe(false)
  })

  it('fetchOne stores the current order', async () => {
    const order = { id: 'o1' } as OrderDto
    mocked.getOrder.mockResolvedValue(order)
    const store = useOrdersStore()

    await store.fetchOne('o1')

    expect(mocked.getOrder).toHaveBeenCalledWith('o1')
    expect(store.current).toEqual(order)
  })

  it('placeOrder sends the payload, stores and returns the order', async () => {
    const order = { id: 'o2' } as OrderDto
    mocked.createOrder.mockResolvedValue(order)
    const store = useOrdersStore()

    const result = await store.placeOrder(shipping)

    expect(mocked.createOrder).toHaveBeenCalledWith(shipping)
    expect(result).toEqual(order)
    expect(store.current).toEqual(order)
    expect(store.loading).toBe(false)
  })

  it('resets loading when placeOrder rejects', async () => {
    mocked.createOrder.mockRejectedValue(new Error('declined'))
    const store = useOrdersStore()

    await expect(store.placeOrder(shipping)).rejects.toThrow('declined')
    expect(store.loading).toBe(false)
  })
})
