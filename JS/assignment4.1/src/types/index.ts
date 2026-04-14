export interface JWTResponse {
  token: string
  refreshToken: string
  firstName: string
  lastName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
}

export interface RefreshTokenRequest {
  jwt: string
  refreshToken: string
}

export interface TodoTask {
  id: string
  taskName: string
  createdDt?: string
  dueDt?: string
  isCompleted: boolean
  isArchived: boolean
  todoPriorityId?: string
  taskSort: number
  todoCategoryId?: string
  todoCategory?: TodoCategory
}

export interface TodoTaskCreate {
  taskName: string
  dueDt?: string
  isCompleted: boolean
  isArchived: boolean
  taskSort: number
  todoCategoryId?: string
  todoPriorityId?: string
}

export interface TodoCategory {
  id: string
  categoryName: string
  categorySort: number
  tag?: string
}

export interface TodoCategoryCreate {
  categoryName: string
  categorySort: number
  tag?: string
}

export interface TodoPriority {
  id: string
  priorityName: string
  prioritySort: number
  syncDt?: string
}

export interface TodoPriorityCreate {
  priorityName: string
  prioritySort: number
}
