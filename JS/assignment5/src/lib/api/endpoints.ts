// Typed endpoint functions over apiFetch. One function per Swagger operation,
// no business logic — just URL + method + body shape.

import { apiFetch, publicFetch, type ApiResult } from "./client";
import type {
  AccountTokenResponse,
  LoginRequest,
  RefreshTokenRequest,
  RegisterRequest,
  TodoCategory,
  TodoCategoryCreate,
  TodoCategoryUpdate,
  TodoPriority,
  TodoPriorityCreate,
  TodoPriorityUpdate,
  TodoTask,
  TodoTaskCreate,
  TodoTaskUpdate,
} from "./contracts";

// ---------- Account (unauthenticated) ----------

export function login(
  body: LoginRequest,
): Promise<ApiResult<AccountTokenResponse>> {
  return publicFetch<AccountTokenResponse>("/api/v1/Account/Login", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function register(
  body: RegisterRequest,
): Promise<ApiResult<AccountTokenResponse>> {
  return publicFetch<AccountTokenResponse>("/api/v1/Account/Register", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function refreshToken(
  body: RefreshTokenRequest,
): Promise<ApiResult<AccountTokenResponse>> {
  return publicFetch<AccountTokenResponse>("/api/v1/Account/RefreshToken", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

// ---------- TodoCategories ----------

export function listCategories(): Promise<ApiResult<TodoCategory[]>> {
  return apiFetch<TodoCategory[]>("/api/v1/TodoCategories");
}

export function getCategory(id: string): Promise<ApiResult<TodoCategory>> {
  return apiFetch<TodoCategory>(`/api/v1/TodoCategories/${id}`);
}

export function createCategory(
  body: TodoCategoryCreate,
): Promise<ApiResult<TodoCategory>> {
  return apiFetch<TodoCategory>("/api/v1/TodoCategories", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function updateCategory(
  id: string,
  body: TodoCategoryUpdate,
): Promise<ApiResult<TodoCategory>> {
  return apiFetch<TodoCategory>(`/api/v1/TodoCategories/${id}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function deleteCategory(id: string): Promise<ApiResult<null>> {
  return apiFetch<null>(`/api/v1/TodoCategories/${id}`, { method: "DELETE" });
}

// ---------- TodoPriorities ----------

export function listPriorities(): Promise<ApiResult<TodoPriority[]>> {
  return apiFetch<TodoPriority[]>("/api/v1/TodoPriorities");
}

export function getPriority(id: string): Promise<ApiResult<TodoPriority>> {
  return apiFetch<TodoPriority>(`/api/v1/TodoPriorities/${id}`);
}

export function createPriority(
  body: TodoPriorityCreate,
): Promise<ApiResult<TodoPriority>> {
  return apiFetch<TodoPriority>("/api/v1/TodoPriorities", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function updatePriority(
  id: string,
  body: TodoPriorityUpdate,
): Promise<ApiResult<TodoPriority>> {
  return apiFetch<TodoPriority>(`/api/v1/TodoPriorities/${id}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function deletePriority(id: string): Promise<ApiResult<null>> {
  return apiFetch<null>(`/api/v1/TodoPriorities/${id}`, { method: "DELETE" });
}

// ---------- TodoTasks ----------

export function listTasks(): Promise<ApiResult<TodoTask[]>> {
  return apiFetch<TodoTask[]>("/api/v1/TodoTasks");
}

export function getTask(id: string): Promise<ApiResult<TodoTask>> {
  return apiFetch<TodoTask>(`/api/v1/TodoTasks/${id}`);
}

export function createTask(
  body: TodoTaskCreate,
): Promise<ApiResult<TodoTask>> {
  return apiFetch<TodoTask>("/api/v1/TodoTasks", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function updateTask(
  id: string,
  body: TodoTaskUpdate,
): Promise<ApiResult<TodoTask>> {
  return apiFetch<TodoTask>(`/api/v1/TodoTasks/${id}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function deleteTask(id: string): Promise<ApiResult<null>> {
  return apiFetch<null>(`/api/v1/TodoTasks/${id}`, { method: "DELETE" });
}
