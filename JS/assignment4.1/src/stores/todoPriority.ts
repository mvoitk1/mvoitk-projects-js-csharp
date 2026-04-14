import { ref } from 'vue'
import { defineStore } from 'pinia'
import { todoPriorityApi } from '@/api/todoPriority'
import type { TodoPriority, TodoPriorityCreate } from '@/types'

export const useTodoPriorityStore = defineStore('todoPriority', () => {
  const items = ref<TodoPriority[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      items.value = (await todoPriorityApi.getAll()).data
    } catch (e) {
      error.value = 'Failed to load priorities'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function create(data: TodoPriorityCreate) {
    loading.value = true
    error.value = null
    try {
      const res = await todoPriorityApi.create(data)
      items.value.push(res.data)
    } catch (e) {
      error.value = 'Failed to create priority'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function update(id: string, data: TodoPriority) {
    loading.value = true
    error.value = null
    try {
      const res = await todoPriorityApi.update(id, data)
      const idx = items.value.findIndex((p) => p.id === id)
      if (idx !== -1) items.value.splice(idx, 1, res.data)
    } catch (e) {
      error.value = 'Failed to update priority'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function remove(id: string) {
    loading.value = true
    error.value = null
    try {
      await todoPriorityApi.remove(id)
      items.value = items.value.filter((p) => p.id !== id)
    } catch (e) {
      error.value = 'Failed to delete priority'
      throw e
    } finally {
      loading.value = false
    }
  }

  return { items, loading, error, fetchAll, create, update, remove }
})
