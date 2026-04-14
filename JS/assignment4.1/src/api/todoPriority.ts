import apiClient from './axios'
import type { TodoPriority, TodoPriorityCreate } from '@/types'

export const todoPriorityApi = {
  getAll: () => apiClient.get<TodoPriority[]>('/api/v1.0/TodoPriorities'),
  create: (data: TodoPriorityCreate) =>
    apiClient.post<TodoPriority>('/api/v1.0/TodoPriorities', data),
  update: (id: string, data: TodoPriority) =>
    apiClient.put<TodoPriority>(`/api/v1.0/TodoPriorities/${id}`, data),
  remove: (id: string) => apiClient.delete(`/api/v1.0/TodoPriorities/${id}`),
}
