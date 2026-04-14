import apiClient from './axios'
import type { TodoCategory, TodoCategoryCreate } from '@/types'

export const todoCategoryApi = {
  getAll: () => apiClient.get<TodoCategory[]>('/api/v1.0/TodoCategories'),
  create: (data: TodoCategoryCreate) =>
    apiClient.post<TodoCategory>('/api/v1.0/TodoCategories', data),
  update: (id: string, data: TodoCategory) =>
    apiClient.put<TodoCategory>(`/api/v1.0/TodoCategories/${id}`, data),
  remove: (id: string) => apiClient.delete(`/api/v1.0/TodoCategories/${id}`),
}
