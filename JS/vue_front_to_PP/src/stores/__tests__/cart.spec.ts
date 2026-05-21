import { describe, it, expect, vi } from 'vitest'
import { useCartStore } from '@/stores/cart'
import type { CartDto } from '@/types'

vi.mock('@/api/cart', () => ({
  getCart: vi.fn(),
  addToCart: vi.fn(),
  updateCartItem: vi.fn(),
  removeCartItem: vi.fn(),
}))

import * as cartApi from '@/api/cart'

const mocked = vi.mocked(cartApi)

function makeCart(over: Partial<CartDto> = {}): CartDto {
  return { id: 'c1', status: 'Active', items: [], total: 0, itemCount: 0, ...over }
}

describe('cart store', () => {
  it('fetchCart toggles loading and stores the cart', async () => {
    const cart = makeCart({ itemCount: 3 })
    let loadingDuringCall = false
    mocked.getCart.mockImplementation(async () => {
      loadingDuringCall = useCartStore().loading
      return cart
    })
    const store = useCartStore()

    await store.fetchCart()

    expect(loadingDuringCall).toBe(true)
    expect(store.loading).toBe(false)
    expect(store.cart).toEqual(cart)
  })

  it('resets loading even when fetchCart rejects', async () => {
    mocked.getCart.mockRejectedValue(new Error('boom'))
    const store = useCartStore()

    await expect(store.fetchCart()).rejects.toThrow('boom')
    expect(store.loading).toBe(false)
  })

  it('addItem replaces the cart with the API result', async () => {
    const cart = makeCart({ itemCount: 1 })
    mocked.addToCart.mockResolvedValue(cart)
    const store = useCartStore()

    await store.addItem({ productVariantId: 'v1', quantity: 1 })

    expect(mocked.addToCart).toHaveBeenCalledWith({ productVariantId: 'v1', quantity: 1 })
    expect(store.cart).toEqual(cart)
  })

  it('updateItem replaces the cart with the API result', async () => {
    const cart = makeCart({ itemCount: 5 })
    mocked.updateCartItem.mockResolvedValue(cart)
    const store = useCartStore()

    await store.updateItem('item1', 5)

    expect(mocked.updateCartItem).toHaveBeenCalledWith('item1', { quantity: 5 })
    expect(store.cart).toEqual(cart)
  })

  it('removeItem calls the API then refetches', async () => {
    mocked.removeCartItem.mockResolvedValue(undefined)
    mocked.getCart.mockResolvedValue(makeCart({ itemCount: 0 }))
    const store = useCartStore()

    await store.removeItem('item1')

    expect(mocked.removeCartItem).toHaveBeenCalledWith('item1')
    expect(mocked.getCart).toHaveBeenCalled()
    expect(store.itemCount).toBe(0)
  })

  it('itemCount reflects the cart and defaults to 0', () => {
    const store = useCartStore()
    expect(store.itemCount).toBe(0)
    store.cart = makeCart({ itemCount: 7 })
    expect(store.itemCount).toBe(7)
  })

  it('clear empties the cart', () => {
    const store = useCartStore()
    store.cart = makeCart({ itemCount: 2 })
    store.clear()
    expect(store.cart).toBeNull()
  })
})
