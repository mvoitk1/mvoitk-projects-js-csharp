import apiClient from './axios'
import type { TodoTask, TodoTaskCreate } from '../types'

export const todoTasksApi = {
  getAll() {
    return apiClient.get<TodoTask[]>('/api/v1.0/TodoTasks')
  },
  getById(id: string) {
    return apiClient.get<TodoTask>(`/api/v1.0/TodoTasks/${id}`)
  },
  create(data: TodoTaskCreate) {
    return apiClient.post<TodoTask>('/api/v1.0/TodoTasks', data)
  },
  update(id: string, data: TodoTask) {
    return apiClient.put<void>(`/api/v1.0/TodoTasks/${id}`, data)
  },
  remove(id: string) {
    return apiClient.delete<void>(`/api/v1.0/TodoTasks/${id}`)
  },
}
