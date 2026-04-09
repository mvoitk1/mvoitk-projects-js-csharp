export interface JWTResponse {
  token: string
  refreshToken: string
  firstName: string
  lastName: string
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RefreshTokenRequest {
  jwt: string
  refreshToken: string
}

export interface TodoTask {
  id: string
  todoTaskName: string
  dueDt?: string
  isCompleted: boolean
  isArchived: boolean
  todoTaskPriority: number
  todoTaskSort: number
  todoCategoryId?: string
  todoCategory?: TodoCategory
}

export interface TodoTaskCreate {
  todoTaskName: string
  dueDt?: string
  isCompleted: boolean
  isArchived: boolean
  todoTaskPriority: number
  todoTaskSort: number
  todoCategoryId?: string
}

export interface TodoCategory {
  id: string
  todoCategoryName: string
  todoCategorySort: number
}

export interface TodoCategoryCreate {
  todoCategoryName: string
  todoCategorySort: number
}
