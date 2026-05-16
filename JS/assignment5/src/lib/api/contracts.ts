// Types mirror the TalTech Swagger surface as of 2026-05-16.
// Base: https://taltech.akaver.com  — all paths under /api/v1/.

export type Guid = string;
export type IsoDateTime = string;

// ---------- Account ----------

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface RefreshTokenRequest {
  jwt: string;
  refreshToken: string;
}

export interface AccountTokenResponse {
  token: string;
  refreshToken: string;
  firstName: string | null;
  lastName: string | null;
}

// ---------- TodoCategory ----------

export interface TodoCategory {
  id: Guid;
  categoryName: string | null;
  categorySort: number;
  syncDt: IsoDateTime;
  tag: string | null;
}

export interface TodoCategoryCreate {
  categoryName: string | null;
  categorySort: number;
  tag: string | null;
}

export type TodoCategoryUpdate = TodoCategory;

// ---------- TodoPriority ----------

export interface TodoPriority {
  id: Guid;
  priorityName: string | null;
  prioritySort: number;
  syncDt: IsoDateTime;
}

export interface TodoPriorityCreate {
  priorityName: string | null;
  prioritySort: number;
  syncDt: IsoDateTime;
}

export type TodoPriorityUpdate = TodoPriority;

// ---------- TodoTask ----------

export interface TodoTask {
  id: Guid;
  taskName: string | null;
  taskSort: number;
  createdDt: IsoDateTime;
  dueDt: IsoDateTime | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: Guid;
  todoPriorityId: Guid;
  syncDt: IsoDateTime;
}

export interface TodoTaskCreate {
  taskName: string | null;
  taskSort: number;
  createdDt: IsoDateTime;
  dueDt: IsoDateTime | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: Guid;
  todoPriorityId: Guid;
}

export type TodoTaskUpdate = TodoTask;

// ---------- Errors ----------

// ASP.NET Core ProblemDetails — what the backend returns on 4xx/5xx for most
// endpoints. Login returns a plain string[] on 404; handle both at the edges.
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  [extension: string]: unknown;
}

export type ApiErrorBody = ProblemDetails | string[] | string | null;
