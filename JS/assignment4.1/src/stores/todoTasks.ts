import { ref } from 'vue'
import { defineStore } from 'pinia'
import { todoTasksApi } from '@/api/todoTasks'
import type { TodoTask, TodoTaskCreate } from '@/types'

export const useTodoTasksStore = defineStore('todoTasks', () => {
  const items = ref<TodoTask[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      items.value = (await todoTasksApi.getAll()).data
    } catch {
      error.value = 'Failed to load tasks'
    } finally {
      loading.value = false
    }
  }

  async function create(data: TodoTaskCreate) {
    loading.value = true
    error.value = null
    try {
      const res = await todoTasksApi.create(data)
      items.value.push(res.data)
    } catch {
      error.value = 'Failed to create task'
    } finally {
      loading.value = false
    }
  }

  async function update(id: string, data: TodoTask) {
    loading.value = true
    error.value = null
    try {
      const res = await todoTasksApi.update(id, data)
      const idx = items.value.findIndex((t) => t.id === id)
      if (idx !== -1) items.value.splice(idx, 1, res.data)
    } catch {
      error.value = 'Failed to update task'
    } finally {
      loading.value = false
    }
  }

  async function remove(id: string) {
    loading.value = true
    error.value = null
    try {
      await todoTasksApi.remove(id)
      items.value = items.value.filter((t) => t.id !== id)
    } catch {
      error.value = 'Failed to delete task'
    } finally {
      loading.value = false
    }
  }

  return { items, loading, error, fetchAll, create, update, remove }
})
