import { defineStore } from 'pinia'
import { ref } from 'vue'
import { todoCategoryApi } from '../api/todoCategory'
import type { TodoCategory, TodoCategoryCreate } from '../types'

export const useTodoCategoryStore = defineStore('todoCategory', () => {
  const items = ref<TodoCategory[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      const { data } = await todoCategoryApi.getAll()
      items.value = data
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch categories'
    } finally {
      loading.value = false
    }
  }

  async function create(category: TodoCategoryCreate) {
    loading.value = true
    error.value = null
    try {
      const { data } = await todoCategoryApi.create(category)
      items.value.push(data)
      return data
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to create category'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function update(id: string, category: TodoCategory) {
    loading.value = true
    error.value = null
    try {
      await todoCategoryApi.update(id, category)
      const idx = items.value.findIndex((c) => c.id === id)
      if (idx !== -1) items.value[idx] = category
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to update category'
      throw e
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
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to delete category'
      throw e
    } finally {
      loading.value = false
    }
  }

  return { items, loading, error, fetchAll, create, update, remove }
})
