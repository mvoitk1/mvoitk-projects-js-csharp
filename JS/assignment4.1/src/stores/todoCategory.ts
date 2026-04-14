import { ref } from 'vue'
import { defineStore } from 'pinia'
import { todoCategoryApi } from '@/api/todoCategory'
import type { TodoCategory, TodoCategoryCreate } from '@/types'

export const useTodoCategoryStore = defineStore('todoCategory', () => {
  const items = ref<TodoCategory[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      items.value = (await todoCategoryApi.getAll()).data
    } catch {
      error.value = 'Failed to load categories'
    } finally {
      loading.value = false
    }
  }

  async function create(data: TodoCategoryCreate) {
    loading.value = true
    error.value = null
    try {
      const res = await todoCategoryApi.create(data)
      items.value.push(res.data)
    } catch {
      error.value = 'Failed to create category'
    } finally {
      loading.value = false
    }
  }

  async function update(id: string, data: TodoCategory) {
    loading.value = true
    error.value = null
    try {
      const res = await todoCategoryApi.update(id, data)
      const idx = items.value.findIndex((c) => c.id === id)
      if (idx !== -1) items.value.splice(idx, 1, res.data)
    } catch {
      error.value = 'Failed to update category'
    } finally {
      loading.value = false
    }
  }

  async function remove(id: string) {
    loading.value = true
    error.value = null
    try {
      await todoCategoryApi.remove(id)
      items.value = items.value.filter((c) => c.id !== id)
    } catch {
      error.value = 'Failed to delete category'
    } finally {
      loading.value = false
    }
  }

  return { items, loading, error, fetchAll, create, update, remove }
})
