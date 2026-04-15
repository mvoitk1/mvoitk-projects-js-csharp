import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getProducts, getProduct } from '@/api/products'
import type { ProductFilters } from '@/api/products'
import type { ProductListItemDto, ProductDto } from '@/types'

export const useProductsStore = defineStore('products', () => {
  const list = ref<ProductListItemDto[]>([])
  const current = ref<ProductDto | null>(null)
  const loading = ref(false)

  async function fetchList(filters: ProductFilters = {}) {
    loading.value = true
    try {
      list.value = await getProducts(filters)
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    loading.value = true
    try {
      current.value = await getProduct(id)
    } finally {
      loading.value = false
    }
  }

  return { list, current, loading, fetchList, fetchOne }
})
