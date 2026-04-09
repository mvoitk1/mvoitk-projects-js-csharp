import { defineStore } from 'pinia'
import { ref } from 'vue'
import { todoPriorityApi } from '../api/todoPriority'
import type { TodoPriority, TodoPriorityCreate } from '../types'

export const useTodoPriorityStore = defineStore('todoPriority', () => {
  const items = ref<TodoPriority[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      const { data } = await todoPriorityApi.getAll()
      items.value = data
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch priorities'
    } finally {
      loading.value = false
    }
  }

  async function create(priority: TodoPriorityCreate) {
    loading.value = true
    error.value = null
    try {
      const { data } = await todoPriorityApi.create(priority)
      items.value.push(data)
      return data
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to create priority'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function update(id: string, priority: TodoPriority) {
    loading.value = true
    error.value = null
    try {
      await todoPriorityApi.update(id, priority)
      const idx = items.value.findIndex((item) => item.id === id)
      if (idx !== -1) items.value[idx] = priority
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to update priority'
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
      items.value = items.value.filter((item) => item.id !== id)
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to delete priority'
      throw e
    } finally {
      loading.value = false
    }
  }

  return { items, loading, error, fetchAll, create, update, remove }
})
