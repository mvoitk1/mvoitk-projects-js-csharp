import { describe, it, expect, vi } from 'vitest'
import { useProductsStore } from '@/stores/products'
import type { ProductListItemDto, ProductDto } from '@/types'

vi.mock('@/api/products', () => ({
  getProducts: vi.fn(),
  getProduct: vi.fn(),
}))

import * as productsApi from '@/api/products'

const mocked = vi.mocked(productsApi)

describe('products store', () => {
  it('fetchList passes filters through and stores the list', async () => {
    const list: ProductListItemDto[] = [
      {
        id: 'p1',
        name: 'Tee',
        gender: 'M',
        isActive: true,
        lowestPrice: 9.99,
        primaryImageUrl: null,
        collectionName: null,
      },
    ]
    mocked.getProducts.mockResolvedValue(list)
    const store = useProductsStore()

    await store.fetchList({ categoryId: 'cat1', gender: 'M' })

    expect(mocked.getProducts).toHaveBeenCalledWith({ categoryId: 'cat1', gender: 'M' })
    expect(store.list).toEqual(list)
    expect(store.loading).toBe(false)
  })

  it('fetchList resets loading on error', async () => {
    mocked.getProducts.mockRejectedValue(new Error('boom'))
    const store = useProductsStore()

    await expect(store.fetchList()).rejects.toThrow('boom')
    expect(store.loading).toBe(false)
  })

  it('fetchOne stores the current product', async () => {
    const product = { id: 'p1', name: 'Tee' } as ProductDto
    mocked.getProduct.mockResolvedValue(product)
    const store = useProductsStore()

    await store.fetchOne('p1')

    expect(mocked.getProduct).toHaveBeenCalledWith('p1')
    expect(store.current).toEqual(product)
  })
})
