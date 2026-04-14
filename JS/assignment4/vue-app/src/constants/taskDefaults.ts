import type { TodoCategoryCreate, TodoPriorityCreate } from '../types'

export const defaultCategories: TodoCategoryCreate[] = [
  { categoryName: 'Work', categorySort: 0 },
  { categoryName: 'Personal', categorySort: 1 },
  { categoryName: 'Study', categorySort: 2 },
]

export const defaultPriorities: TodoPriorityCreate[] = [
  { priorityName: 'Low', prioritySort: 0 },
  { priorityName: 'Medium', prioritySort: 1 },
  { priorityName: 'High', prioritySort: 2 },
]
