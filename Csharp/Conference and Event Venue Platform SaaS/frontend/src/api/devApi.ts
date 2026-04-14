import { api } from './apiClient';

export interface BootstrapRequest {
  companyName: string;
  companySlug: string;
  email: string;
  password: string;
  role: string;
}

export interface BootstrapResponse {
  companyId: string;
  userId: string;
  companySlug: string;
  role: string;
}

/**
 * Dev-only: Bootstrap a tenant by creating company, user, and membership in one call.
 * This endpoint is only available in development builds.
 *
 * Backend expects JSON body with:
 * - companyName
 * - companySlug
 * - email
 * - password
 * - role
 */
export async function bootstrapTenant(
  payload: BootstrapRequest,
): Promise<BootstrapResponse> {
  // Send as JSON body (consistent with rest of API)
  return api.post<BootstrapResponse>('/dev/bootstrap', {
    companyName: payload.companyName,
    companySlug: payload.companySlug,
    email: payload.email,
    password: payload.password,
    role: payload.role,
  });
}
