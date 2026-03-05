/**
 * =====================================================================================
 * AUTHENTICATION API - How Users Log In and Register
 * =====================================================================================
 * 
 * This file contains functions for authentication - the process of proving
 * who you are (login) and creating a new account (register).
 * 
 * =====================================================================================
 * KEY CONCEPTS EXPLAINED FOR STUDENTS:
 * =====================================================================================
 * 
 * 1. WHAT IS AUTHENTICATION?
 *    Authentication is the process of verifying that someone is who they claim to be.
 *    Common methods:
 *    - Username/password
 *    - Email/password
 *    - OAuth (Google, Facebook login)
 *    - Biometrics (fingerprint, face)
 * 
 * 2. WHAT IS A JWT TOKEN?
 *    JWT (JSON Web Token) is a compact, URL-safe token format.
 *    After logging in, the server gives the client a token.
 *    The client then includes this token in every request to prove identity.
 *    
 *    Think of it like a hotel key card:
 *    - You check in (login) and get a key card (token)
 *    - You show the key card to access your room (make API requests)
 *    - When the key expires, you need a new one (refresh token)
 * 
 * 3. WHAT IS "/auth/login" VS "/auth/register"?
 *    - /auth/login = "I already have an account, let me in"
 *    - /auth/register = "Create a new account for me"
 * 
 * 4. WHAT DOES "tenant-scoped" MEAN?
 *    In this system, some endpoints are "tenant-scoped" - they require a company
 *    context. Authentication endpoints are "global" - they work before you pick
 *    a company.
 * 
 *    Example:
 *    - /auth/login (global) - Works without a company
 *    - /my-company/spaces (tenant-scoped) - Needs company context
 * 
 * 5. WHAT IS A "credentials" OBJECT?
 *    It's a container for login information. Usually:
 *    {
 *      email: "user@example.com",
 *      password: "secretpassword"
 *    }
 * 
 * 6. WHAT IS "Promise<LoginResponse>"?
 *    This function returns a Promise that will eventually resolve to a LoginResponse.
 *    The Promise handles the async nature of network requests.
 * 
 * 7. WHAT IS "/auth/me"?
 *    This endpoint gets the current user's information. It's used to:
 *    - Check if user is logged in
 *    - Get user's profile info
 *    - Get list of companies user belongs to
 * 
 * =====================================================================================
 */

import { api } from './apiClient'
import { LoginRequest, LoginResponse, RegisterRequest, RegisterResponse, MeResponse } from '../types/apiTypes'

/**
 * loginUser - Authenticate with email and password
 * 
 * This sends the user's credentials to the server.
 * If correct, the server returns a JWT token and user info.
 * 
 * @param credentials - Object containing email and password
 * @returns Promise<LoginResponse> - Contains token, userId, and email
 * 
 * Example usage:
 *   const response = await loginUser({
 *     email: "user@example.com",
 *     password: "mypassword"
 *   })
 *   console.log(response.token) // "eyJhbGciOiJIUz..."
 */
export async function loginUser(credentials: LoginRequest): Promise<LoginResponse> {
  // POST to /auth/login endpoint
  // The api.post function sends a POST request with the credentials as JSON body
  return api.post<LoginResponse>('/auth/login', credentials)
}

/**
 * registerUser - Create a new user account
 * 
 * This creates a new account. The payload determines what kind of account:
 * - Owner mode: Creates a new company + user account
 * - User mode: Creates user account that joins existing company
 * - No mode: Creates user account without company
 * 
 * @param payload - Registration data
 * @returns Promise<RegisterResponse> - Contains userId, email, and company info
 * 
 * Example usage (create owner + company):
 *   await registerUser({
 *     email: "newuser@example.com",
 *     password: "securepassword",
 *     mode: "Owner",
 *     companyName: "My Venue",
 *     companySlug: "my-venue"
 *   })
 */
export async function registerUser(payload: RegisterRequest): Promise<RegisterResponse> {
  // POST to /auth/register endpoint
  return api.post<RegisterResponse>('/auth/register', payload)
}

/**
 * getMe - Get current user information
 * 
 * This endpoint requires authentication (must include JWT token).
 * It returns the user's profile and list of companies they belong to.
 * 
 * This is commonly used:
 * - On app startup to restore user session
 * - To check if token is still valid
 * - To get list of companies user can access
 * 
 * @returns Promise<MeResponse> - Contains userId, email, and companies array
 * 
 * Example response:
 *   {
 *     userId: "12345",
 *     email: "user@example.com",
 *     hasCompanies: true,
 *     companies: [
 *       { companySlug: "my-venue", companyName: "My Venue", role: "Owner" }
 *     ]
 *   }
 */
export async function getMe(): Promise<MeResponse> {
  // GET from /auth/me endpoint
  // This is a protected endpoint - the api client automatically adds the JWT token
  return api.get<MeResponse>('/auth/me')
}
