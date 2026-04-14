/**
 * SPACES API - This file contains functions to talk to the backend about "Spaces" (rooms/venues)
 * 
 * 
 * =====================================================================================
 * WHAT IS ALL THIS STUFF? A STUDENT'S GUIDE TO READING THIS CODE
 * =====================================================================================
 * 
 * First, let's understand the whole line:
 *   export async function getSpace(companySlug: string, id: string): Promise<Space>
 * 
 * 1. "export" - This keyword makes this function available to OTHER FILES in our project.
 *    Think of it like making a function public. Without "export", only code in THIS FILE
 *    could use this function. With "export", other files can import it like:
 *    import { getSpace } from './spacesApi'
 * 
 * 2. "async" - This keyword means "asynchronous" - the function does something that takes time
 *    (like waiting for the server to respond). While waiting, JavaScript can do other things.
 *    Without async, the whole app would freeze while waiting for the server!
 *    
 *    Example: When you call getSpace(), it sends a request to the server and waits for the
 *    response. With async, the browser says "I'll go do other stuff and check back later"
 *    instead of freezing.
 * 
 * 3. "function" - This declares that we're creating a function (a reusable piece of code).
 *    
 * 4. "getSpace" - The name of our function. We call this by using getSpace() later.
 * 
 * 5. "(companySlug: string, id: string)" - These are PARAMETERS (inputs) the function needs.
 *    - "companySlug" is a string (text) that identifies which company owns this space
 *    - "id" is a string that uniquely identifies the space
 *    - The ": string" part is TypeScript telling us the EXPECTED type of each parameter
 * 
 * 6. ": Promise<Space>" - This is the RETURN TYPE. The function promises to eventually
 *    give back a "Space" object. The "Promise" part means it won't give it immediately -
 *    it will give it in the future (when the server responds).
 * 
 *    Think of it like ordering food at a restaurant:
 *    - Without Promise: They make you wait at the kitchen until food is ready (freezes app)
 *    - With Promise: They give you a number/buzzer and you can do other things until
 *      your number is called (app keeps running)
 * 
 * 7. "<Space>" - This is called a GENERIC. It's like a template parameter. It tells
 *    TypeScript exactly what kind of object will be in the Promise. In this case, it's
 *    a "Space" object that we defined in our types file.
 * 
 * =====================================================================================
 */

import { api } from './apiClient'
// Import the Space, CreateSpaceRequest, etc. types from our types file
// This gives us type safety - TypeScript will warn us if we use these incorrectly!
import type { Space, CreateSpaceRequest, UpdateSpaceRequest, CreateEntityResponse } from '../types/apiTypes'

/**
 * GET ALL SPACES FOR A COMPANY
 * 
 * This function fetches all spaces (rooms/venues) belonging to a specific company.
 * 
 * @param companySlug - The URL-friendly identifier for the company (e.g., "my-company")
 * @returns Promise<Space[]> - A promise that will eventually return an ARRAY of Space objects
 * 
 * Example usage:
 *   const spaces = await getSpaces('my-company')
 *   console.log(spaces[0].name) // "Conference Room A"
 */
export async function getSpaces(companySlug: string): Promise<Space[]> {
  // encodeURIComponent() - Converts special characters in the companySlug to a format
  // that's safe to include in a URL. For example, "John's Company" becomes "John%27s%20Company"
  // This prevents URL errors!
  const encodedCompanySlug = encodeURIComponent(companySlug)
  
  // api.get() - This is our helper function that makes a GET request to the backend.
  // The <Array<{...}>> part is telling TypeScript what shape of data we expect back.
  // We're saying "we expect an array of objects with these properties"
  const response = await api.get<Array<{
    id: string
    name: string
    capacity: number
    notes: string | null
    isActive: boolean
  }>>(`/${encodedCompanySlug}/spaces`)

  // Return the response (mapped to our Space type)
  // The .map() function transforms each item in the array
  return response.map(s => ({
    ...s, // Copy all properties from the server response
  }))
}

/**
 * GET A SINGLE SPACE BY ID
 * 
 * Fetches details for one specific space.
 * 
 * @param companySlug - The company identifier
 * @param id - The unique ID of the space
 * @returns Promise<Space> - A promise that will return a single Space object
 */
export async function getSpace(companySlug: string, id: string): Promise<Space> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  
  // Note: This returns a single object, not an array (no Array<>)
  const response = await api.get<{
    id: string
    name: string
    capacity: number
    notes: string | null
    isActive: boolean
  }>(`/${encodedCompanySlug}/spaces/${id}`)

  return response
}

/**
 * CREATE A NEW SPACE
 * 
 * Sends data to the backend to create a new space (room/venue).
 * 
 * @param companySlug - The company identifier
 * @param payload - The data for the new space (name, capacity, notes)
 * @returns Promise<CreateEntityResponse> - Contains the ID of the newly created space
 * 
 * POST vs GET:
 * - GET = Retrieving data (reading)
 * - POST = Sending data to create something new (writing)
 */
export async function createSpace(
  companySlug: string,
  payload: CreateSpaceRequest
): Promise<CreateEntityResponse> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  
  // api.post() sends a POST request with a body (the data to create)
  const response = await api.post<CreateEntityResponse>(`/${encodedCompanySlug}/spaces`, payload)

  return response
}

/**
 * UPDATE AN EXISTING SPACE
 * 
 * Sends updated data to modify an existing space.
 * 
 * @param companySlug - The company identifier  
 * @param id - The ID of the space to update
 * @param payload - The new data for the space
 * @returns Promise<Space> - Returns the updated space
 * 
 * PUT vs POST:
 * - POST = Create something new
 * - PUT = Update/replace something that already exists
 */
export async function updateSpace(
  companySlug: string,
  id: string,
  payload: UpdateSpaceRequest
): Promise<Space> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  const response = await api.put<{
    id: string
    name: string
    capacity: number
    notes: string | null
    isActive: boolean
  }>(`/${encodedCompanySlug}/spaces/${id}`, payload)

  return response
}

/**
 * DEACTIVATE (SOFT DELETE) A SPACE
 * 
 * Instead of permanently deleting a space, we mark it as inactive.
 * This preserves historical data (bookings, invoices, etc.) while
 * hiding the space from new bookings.
 * 
 * @param companySlug - The company identifier
 * @param id - The ID of the space to deactivate
 * @returns Promise<{ id, name, isActive }> - Returns the updated space info
 * 
 * "Soft delete" vs "Hard delete":
 * - Hard delete: Completely remove from database (dangerous!)
 * - Soft delete: Just mark as inactive (reversible, keeps history)
 */
export async function deactivateSpace(
  companySlug: string,
  id: string
): Promise<{ id: string; name: string; isActive: boolean }> {
  const encodedCompanySlug = encodeURIComponent(companySlug)
  
  // Even though we're modifying data, we use POST here because the API
  // endpoint is designed as a "action" (deactivating something)
  const response = await api.post<{
    id: string
    name: string
    isActive: boolean
  }>(`/${encodedCompanySlug}/spaces/${id}/deactivate`, {})

  return response
}
