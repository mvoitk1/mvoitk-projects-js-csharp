import { describe, it, expect, vi } from 'vitest'
import { useCategoriesStore } from '@/stores/categories'
import { useCollectionsStore } from '@/stores/collections'
import type { CategoryDto, CollectionDto } from '@/types'

vi.mock('@/api/categories', () => ({ getCategories: vi.fn() }))
vi.mock('@/api/collections', () => ({ getCollections: vi.fn() }))

import * as categoriesApi from '@/api/categories'
import * as collectionsApi from '@/api/collections'

describe('categories store', () => {
  it('fetchCategories stores the tree and resets loading', async () => {
    const tree = [{ id: 'c1', name: 'Tops' }] as CategoryDto[]
    vi.mocked(categoriesApi).getCategories.mockResolvedValue(tree)
    const store = useCategoriesStore()

    await store.fetchCategories()

    expect(store.tree).toEqual(tree)
    expect(store.loading).toBe(false)
  })

  it('resets loading when the fetch rejects', async () => {
    vi.mocked(categoriesApi).getCategories.mockRejectedValue(new Error('boom'))
    const store = useCategoriesStore()

    await expect(store.fetchCategories()).rejects.toThrow('boom')
    expect(store.loading).toBe(false)
  })
})

describe('collections store', () => {
  it('fetchCollections stores the list and resets loading', async () => {
    const list = [{ id: 'k1', name: 'Summer' }] as CollectionDto[]
    vi.mocked(collectionsApi).getCollections.mockResolvedValue(list)
    const store = useCollectionsStore()

    await store.fetchCollections()

    expect(store.list).toEqual(list)
    expect(store.loading).toBe(false)
  })
})
