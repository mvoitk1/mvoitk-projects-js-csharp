import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getCategories } from '@/api/categories'
import type { CategoryDto } from '@/types'

export const useCategoriesStore = defineStore('categories', () => {
  const tree = ref<CategoryDto[]>([])
  const loading = ref(false)

  async function fetchCategories() {
    loading.value = true
    try {
      tree.value = await getCategories()
    } finally {
      loading.value = false
    }
  }

  return { tree, loading, fetchCategories }
})
