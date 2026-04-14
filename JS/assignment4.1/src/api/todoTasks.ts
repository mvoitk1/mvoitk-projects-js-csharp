import apiClient from './axios'
import type { TodoTask, TodoTaskCreate } from '@/types'

export const todoTasksApi = {
  getAll: () => apiClient.get<TodoTask[]>('/api/v1.0/TodoTasks'),
  getById: (id: string) => apiClient.get<TodoTask>(`/api/v1.0/TodoTasks/${id}`),
  create: (data: TodoTaskCreate) => apiClient.post<TodoTask>('/api/v1.0/TodoTasks', data),
  update: (id: string, data: TodoTask) =>
    apiClient.put<TodoTask>(`/api/v1.0/TodoTasks/${id}`, data),
  remove: (id: string) => apiClient.delete(`/api/v1.0/TodoTasks/${id}`),
}
