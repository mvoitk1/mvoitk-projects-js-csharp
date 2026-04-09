import apiClient from './axios'
import type { TodoPriority, TodoPriorityCreate } from '../types'

export const todoPriorityApi = {
  getAll() {
    return apiClient.get<TodoPriority[]>('/api/v1.0/TodoPriorities')
  },
  getById(id: string) {
    return apiClient.get<TodoPriority>(`/api/v1.0/TodoPriorities/${id}`)
  },
  create(data: TodoPriorityCreate) {
    return apiClient.post<TodoPriority>('/api/v1.0/TodoPriorities', data)
  },
  update(id: string, data: TodoPriority) {
    return apiClient.put<void>(`/api/v1.0/TodoPriorities/${id}`, data)
  },
  remove(id: string) {
    return apiClient.delete<void>(`/api/v1.0/TodoPriorities/${id}`)
  },
}
