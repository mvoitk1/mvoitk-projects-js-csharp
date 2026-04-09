import { defineStore } from 'pinia'
import { ref } from 'vue'
import { todoTasksApi } from '../api/todoTasks'
import type { TodoTask, TodoTaskCreate } from '../types'

export const useTodoTasksStore = defineStore('todoTasks', () => {
  const items = ref<TodoTask[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      const { data } = await todoTasksApi.getAll()
      items.value = data
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch tasks'
    } finally {
      loading.value = false
    }
  }

  async function create(task: TodoTaskCreate) {
    loading.value = true
    error.value = null
    try {
      const { data } = await todoTasksApi.create(task)
      items.value.push(data)
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to create task'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function update(id: string, task: TodoTask) {
    loading.value = true
    error.value = null
    try {
      await todoTasksApi.update(id, task)
      const idx = items.value.findIndex((t) => t.id === id)
      if (idx !== -1) items.value[idx] = task
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to update task'
      throw e
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
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to delete task'
      throw e
    } finally {
      loading.value = false
    }
  }

  return { items, loading, error, fetchAll, create, update, remove }
})
