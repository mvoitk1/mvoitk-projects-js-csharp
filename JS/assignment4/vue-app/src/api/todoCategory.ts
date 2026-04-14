import apiClient from './axios'
import type { TodoCategory, TodoCategoryCreate } from '../types'

export const todoCategoryApi = {
  getAll() {
    return apiClient.get<TodoCategory[]>('/api/v1.0/TodoCategories')
  },
  getById(id: string) {
    return apiClient.get<TodoCategory>(`/api/v1.0/TodoCategories/${id}`)
  },
  create(data: TodoCategoryCreate) {
    return apiClient.post<TodoCategory>('/api/v1.0/TodoCategories', data)
  },
  update(id: string, data: TodoCategory) {
    return apiClient.put<void>(`/api/v1.0/TodoCategories/${id}`, data)
  },
  remove(id: string) {
    return apiClient.delete<void>(`/api/v1.0/TodoCategories/${id}`)
  },
}
