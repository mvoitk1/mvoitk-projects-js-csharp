/**
 * =====================================================================================
 * API TYPES - The Data Blueprints of Our Application
 * =====================================================================================
 * 
 * This file contains TYPE DEFINITIONS (also called "interfaces" in TypeScript).
 * Think of these as blueprints or contracts - they define what data should look like.
 * 
 * =====================================================================================
 * KEY CONCEPTS EXPLAINED FOR STUDENTS:
 * =====================================================================================
 * 
 * 1. WHAT IS TypeScript?
 *    TypeScript is JavaScript with "type annotations" - it's like adding labels to your
 *    data that tell the computer what kind of information you're working with.
 *    
 *    JavaScript (no types):
 *      function greet(name) { return "Hello, " + name }
 *    
 *    TypeScript (with types):
 *      function greet(name: string): string { return "Hello, " + name }
 * 
 * 2. WHAT IS AN INTERFACE?
 *    An interface defines what properties an object should have. It's like a form
 *    template - you can only fill in the fields that are on the form.
 *    
 *    Example:
 *      interface Person {
 *        name: string      // Must have a name (text)
 *        age: number       // Must have an age (number)
 *        email?: string   // The ? means optional - can be included or not
 *      }
 * 
 * 3. WHAT IS "export interface"?
 *    The "export" keyword makes this interface available to other files.
 *    Other files can import it like:
 *      import { Space } from './apiTypes'
 * 
 * 4. WHAT DOES ": string" MEAN?
 *    This is a TYPE ANNOTATION - it tells TypeScript what type of value to expect.
 *    Common types:
 *    - string  = Text, like "Hello" or "John's Company"
 *    - number  = Numbers, like 42 or 3.14
 *    - boolean = true or false
 *    - null    = Nothing (intentionally empty)
 *    - undefined = Not yet assigned
 * 
 * 5. WHAT IS "string | null"?
 *    This is a UNION TYPE - the value can be EITHER type.
 *    "string | null" means "either a string or null"
 *    
 *    Example: notes could be "Great room!" or null if no notes exist
 * 
 * 6. WHAT IS "?: number" (optional property)?
 *    The ? makes a property OPTIONAL - it doesn't have to be provided.
 *    
 *    Example:
 *      interface User {
 *        name: string     // REQUIRED - must provide this
 *        age?: number     // OPTIONAL - can omit this
 *      }
 *      
 *      // Both are valid:
 *      const user1: User = { name: "John" }
 *      const user2: User = { name: "Jane", age: 25 }
 * 
 * 7. WHAT IS "type" vs "interface"?
 *    Both define the shape of objects. Differences:
 *    - interface can be extended, type cannot
 *    - type can do more complex things (unions, intersections)
 *    
 *    For simple objects, you can use either. This project uses interface for objects.
 * 
 * 8. WHAT IS "Array<T>" OR "T[]"?
 *    Both mean "an array of T" where T is some type.
 *    Array<string> is the same as string[]
 *    
 *    Example:
 *      const names: string[] = ["Alice", "Bob", "Charlie"]
 *      const names2: Array<string> = ["Alice", "Bob", "Charlie"]  // Same thing
 * 
 * 9. WHAT IS "class"?
 *    A class is a blueprint for creating objects. It can have:
 *    - Properties (variables)
 *    - Methods (functions)
 *    - Constructor (special function called when creating)
 *    
 *    Example:
 *      class Person {
 *        name: string
 *        constructor(name: string) {
 *          this.name = name  // "this" refers to the current object
 *        }
 *        greet() { return "Hello, " + this.name }
 *      }
 *      
 *      const person = new Person("John")
 *      person.greet()  // Returns "Hello, John"
 * 
 * 10. WHAT IS "extends"?
 *    "extends" means "inherits from" - the new class gets all properties/methods
 *    from the parent class, plus its own.
 *    
 *    Example:
 *      class Animal {
 *        name: string
 *      }
 *      
 *      class Dog extends Animal {
 *        bark() { return "Woof!" }
 *      }
 *      
 *      const dog = new Dog()
 *      dog.name  // Exists from Animal
 *      dog.bark() // Exists from Dog
 * 
 * =====================================================================================
 */

// ==================== Auth Types ====================

/**
 * LoginRequest - Data needed to log in
 * 
 * This interface defines what data we send to the server when a user logs in.
 */
export interface LoginRequest {
  email: string      // User's email address (required)
  password: string   // User's password (required)
}

/**
 * LoginResponse - Data returned after successful login
 * 
 * The server sends these fields after verifying credentials.
 */
export interface LoginResponse {
  token: string      // JWT token for authentication (keep this secret!)
  userId: string     // Unique identifier for the user
  email: string      // User's email address
}

/**
 * RegisterRequest - Data needed to create a new account
 * 
 * Some fields are optional (marked with ?) - they only apply in certain cases.
 */
export interface RegisterRequest {
  email: string
  password: string
  mode?: 'Owner' | 'User' | null  // Omit for account-only signup (optional)
  companyName?: string            // Required when mode === 'Owner' (optional but sometimes needed)
  companySlug?: string            // Required when mode === 'Owner' or mode === 'User'
  role?: string                  // Optional, only honored internally
}

/**
 * RegisterResponse - Data returned after successful registration
 */
export interface RegisterResponse {
  userId: string
  email: string
  companySlug: string | null   // Can be a string or null
  membershipRole: string | null
}

// ==================== Me/Profile Types ====================

/**
 * UserCompanyDto - A company that a user belongs to
 * 
 * DTO stands for "Data Transfer Object" - it's just a container for data.
 */
export interface UserCompanyDto {
  companySlug: string   // URL-friendly company ID (e.g., "my-company")
  companyName: string   // Display name (e.g., "My Company")
  role: string          // User's role in this company (e.g., "Owner", "Admin")
}

/**
 * MeResponse - Current user's profile information
 * 
 * This tells us who the logged-in user is and what companies they belong to.
 */
export interface MeResponse {
  userId: string
  email: string
  hasCompanies: boolean           // Does the user belong to any companies?
  companies: UserCompanyDto[]    // Array of companies the user belongs to
}

// ==================== Create Company Types ====================

/**
 * CreateCompanyRequest - Data to create a new company
 */
export interface CreateCompanyRequest {
  name: string   // Company display name
  slug: string   // URL-friendly identifier
}

/**
 * CreateCompanyResponse - Data returned after creating a company
 */
export interface CreateCompanyResponse {
  companyId: string
  companyName: string
  companySlug: string
  role: string
}

// ==================== Public Companies (Venue Directory) Types ====================

/**
 * PublicCompanyDto - Company info shown to the public (customers)
 * 
 * This is "public" data - info that anyone can see.
 */
export interface PublicCompanyDto {
  companySlug: string
  companyName: string
  plan: CompanyPlan    // The subscription plan they're on
}

/**
 * PublicCompaniesResponse - List of all public companies
 */
export interface PublicCompaniesResponse {
  companies: PublicCompanyDto[]
}

// ==================== Public Spaces Types ====================

/**
 * PublicSpaceDto - Space info shown to the public
 * 
 * Less detailed than internal Space - we don't show everything to customers.
 */
export interface PublicSpaceDto {
  id: string
  name: string
  capacity: number
  notes: string | null   // Can be text or "no notes"
}

// ==================== Customer Booking Request Types ====================

/**
 * CreateBookingRequestRequest - Data for a customer to request a booking
 * 
 * This is what a customer fills out when they want to book a space.
 */
export interface CreateBookingRequestRequest {
  contactName: string           // Who is making the booking
  contactEmail: string         // Email for confirmation
  contactPhone?: string        // Optional phone number
  notes?: string               // Optional special requests
  startUtc: string             // Start time in UTC (ISO format: "2024-01-15T09:00:00Z")
  endUtc: string               // End time in UTC
  spaceIds: string[]           // Which spaces they want to book
}

/**
 * CreateBookingRequestResponse - Confirmation after submitting request
 */
export interface CreateBookingRequestResponse {
  bookingId: string   // The ID of the newly created booking request
}

// ==================== Billing Types ====================

/**
 * CompanyPlan - The subscription tier
 * 
 * This is a "literal type" - only these exact values are allowed.
 * It's like an enum but simpler.
 */
export type CompanyPlan = 'Free' | 'Starter' | 'Professional' | 'Enterprise'

/**
 * PlanUsageResponse - How much of the plan limit is being used
 * 
 * This helps show users "you're using 5 of 10 spaces available"
 */
export interface PlanUsageResponse {
  companyName: string
  companySlug: string
  plan: CompanyPlan
  maxSpaces: number              // Maximum allowed on this plan
  currentSpaces: number          // Currently created
  maxBookingsPerMonth: number    // Maximum bookings per month
  currentBookingsThisMonth: number // Bookings used this month
  monthStartUtc: string         // Start of billing period
  monthEndUtc: string           // End of billing period
}

// ==================== API Error Types ====================

/**
 * ApiErrorResponse - Standard error format from the API
 * 
 * When something goes wrong, the server sends this structure.
 */
export interface ApiErrorResponse {
  code: string                         // Error code (e.g., "invalid_credentials")
  error: string                        // Human-readable error message
  details?: Record<string, string[]>   // Field-specific validation errors
}

/**
 * ApiError - Custom Error class for API errors
 * 
 * We extend the built-in Error class to add our own properties.
 * 
 * WHAT IS "class" AND "extends"?
 * See the explanation at the top of this file!
 */
export class ApiError extends Error {
  // These are "constructor parameters with visibility modifiers"
  // They automatically become properties of the class
  constructor(
    public code: string,               // Error code
    message: string,                  // Error message (passed to parent Error)
    public details?: Record<string, string[]>,  // Extra error details
    public statusCode?: number         // HTTP status code
  ) {
    super(message)  // Call the parent Error class constructor
    this.name = 'ApiError'  // Set the error name
  }
}

// ==================== Error Code Mapping ====================

/**
 * ERROR_CODE_MESSAGES - Translates error codes to user-friendly messages
 * 
 * This maps technical error codes to messages users can understand.
 * 
 * WHAT IS "Record<string, string>"?
 * This is TypeScript shorthand for: an object where all keys are strings
 * and all values are also strings.
 * 
 * Example:
 *   {
 *     "invalid_credentials": "Invalid email or password",
 *     "token_expired": "Your session has expired..."
 *   }
 */
export const ERROR_CODE_MESSAGES: Record<string, string> = {
  // Auth errors
  invalid_credentials: 'Invalid email or password',
  token_expired: 'Your session has expired. Please log in again',
  token_invalid: 'Invalid authentication token',
  
  // Permission errors
  forbidden: 'You do not have permission to perform this action',
  insufficient_permissions: 'You do not have permission to perform this action',
  
  // Plan/Feature errors
  plan_required: 'This feature requires a plan upgrade',
  plan_limit_exceeded: 'You have reached your plan limit',
  
  // Booking errors
  booking_conflict: 'This time slot is already booked',
  
  // Payment errors
  payment_overpay: 'Payment amount exceeds the invoice amount',
  payment_failed: 'Payment processing failed',
  
  // Invoice errors
  invoice_email_cooldown: 'Please wait before sending another invoice email',
  invoice_already_paid: 'This invoice has already been paid',
  invoice_already_voided: 'This invoice has been voided',
  
  // Validation errors
  validation_error: 'Please check your input and try again',
  
  // Generic errors
  not_found: 'The requested resource was not found',
  conflict: 'A conflict occurred. Please try again',
  internal_error: 'An unexpected error occurred. Please try again',
}

/**
 * getErrorMessage - Look up the user-friendly message for an error code
 * 
 * @param code - The error code from the API
 * @returns The user-friendly message, or a default if not found
 */
export function getErrorMessage(code: string): string {
  // Look up the code in our mapping, or return a default
  return ERROR_CODE_MESSAGES[code] || 'An unexpected error occurred'
}

// ==================== User Types ====================

/**
 * User - Basic user information
 */
export interface User {
  id: string           // Unique user ID
  email: string       // User's email
  companySlug: string // Primary company they're associated with
}

// ==================== Company Types ====================

/**
 * Company - A venue/business in the platform
 */
export interface Company {
  id: string           // Unique company ID
  name: string         // Display name
  slug: string         // URL-friendly identifier
  plan: CompanyPlan    // Subscription plan
}

/**
 * CreateEntityResponse - Response after creating something
 * 
 * Used when we only need to know the ID of what was created.
 */
export interface CreateEntityResponse {
  id: string   // The ID of the newly created entity
}

// ==================== Space Types ====================

/**
 * Space - A room/venue that can be booked
 * 
 * This is the main entity in our system - venues have spaces (rooms).
 */
export interface Space {
  id: string            // Unique space ID
  name: string         // Display name (e.g., "Conference Room A")
  capacity: number     // How many people it fits
  notes: string | null // Additional notes, or null if none
  isActive: boolean    // Is this space available for booking?
}

/**
 * CreateSpaceRequest - Data to create a new space
 */
export interface CreateSpaceRequest {
  name: string
  capacity: number
  notes?: string        // Optional
}

/**
 * UpdateSpaceRequest - Data to update a space
 */
export interface UpdateSpaceRequest {
  name: string
  capacity: number
  notes?: string
}

// ==================== Client Types ====================

/**
 * Client - A customer of the venue
 * 
 * This represents a person or company that books spaces.
 */
export interface Client {
  id: string
  name: string
  email?: string        // Optional - not all clients have email
  notes?: string
  companyId: string    // Which company owns this client record
  createdUtc?: string   // When the client was created
}

export interface CreateClientRequest {
  name: string
  email?: string
  notes?: string
}

export interface UpdateClientRequest {
  name: string
  email?: string
  notes?: string
}

// ==================== Booking Types ====================

/**
 * BookingStatus - Possible states of a booking
 * 
 * Using "type" with a union of string literals restricts values to exactly these.
 */
export type BookingStatus = 'Pending' | 'Confirmed'

/**
 * Booking - A reservation of a space
 */
export interface Booking {
  id: string
  clientId: string            // Who made the booking
  title: string              // Booking title/name
  startUtc: string           // Start time
  endUtc: string             // End time
  attendeeCount: number      // How many people
  isCancelled: boolean       // Has it been cancelled?
  spaceIds: string[]         // Which spaces are booked
  totalAmount: number        // Total price
  spaceConfigurationId?: string  // Optional configuration
  status: BookingStatus      // Current status
  cancelledUtc?: string      // When it was cancelled (optional)
  cancelReason?: string      // Why it was cancelled (optional)
  createdByUserId?: string  // Who created it
  confirmedByUserId?: string // Who confirmed it
  cancelledByUserId?: string // Who cancelled it
}

/**
 * BookingDetails - Extended booking information with related data
 */
export interface BookingDetails {
  id: string
  clientId: string
  clientName: string         // Included for display
  title: string
  startUtc: string
  endUtc: string
  attendeeCount: number
  isCancelled: boolean
  totalAmount: number
  spaceConfigurationId?: string
  spaces: SpaceSummary[]     // Included space details
  status: BookingStatus
  cancelledUtc?: string
  cancelReason?: string
  createdByUserId?: string
  confirmedByUserId?: string
  cancelledByUserId?: string
}

/**
 * SpaceSummary - Minimal space info for displaying in lists
 */
export interface SpaceSummary {
  id: string
  name: string
}

/**
 * CreateBookingRequest - Data to create a new booking
 */
export interface CreateBookingRequest {
  clientId: string
  title: string
  startUtc: string
  endUtc: string
  attendeeCount: number
  spaceIds: string[]
  spaceConfigurationId?: string
}

/**
 * BookingConflictResponse - Info about a scheduling conflict
 */
export interface BookingConflictResponse {
  error: string
  conflictingBookingIds: string[]
  conflictingSpaceIds: string[]
}

// ==================== Invoice Types ====================

/**
 * InvoiceStatus - Possible states of an invoice
 */
export type InvoiceStatus = 'Draft' | 'Issued' | 'Void' | 'Sent' | 'Paid'

/**
 * InvoiceItem - A line item on an invoice
 */
export interface InvoiceItem {
  id: string
  description: string     // What was charged
  quantity: number        // How many units
  unitPrice: number      // Price per unit
  lineTotal: number       // quantity * unitPrice
}

/**
 * Invoice - A bill sent to a client
 */
export interface Invoice {
  id: string
  bookingId: string
  createdUtc: string
  status: InvoiceStatus
  currency: string        // e.g., "USD", "EUR"
  subtotalAmount: number
  invoiceNumber: number   // Human-readable invoice number
  invoiceNumberText: string  // Formatted version
  issuedUtc?: string      // When it was issued
  voidedUtc?: string      // When it was voided
  sentUtc?: string        // When it was sent to client
  amountPaid: number      // How much has been paid
  amountDue: number       // How much is still owed
  isPaid: boolean         // Is it fully paid?
  paidUtc?: string        // When it was fully paid
}

/**
 * InvoiceDetail - Full invoice with all details
 */
export interface InvoiceDetail extends Invoice {
  createdByUserId: string
  items: InvoiceItem[]
  issuedByUserId?: string
  voidedByUserId?: string
  sentByUserId?: string
  paidByUserId?: string
}

/**
 * Payment - A payment recorded against an invoice
 */
export interface Payment {
  id: string
  invoiceId: string
  amount: number
  paidUtc: string       // When payment was made
  method: string        // e.g., "Credit Card", "Cash"
  reference?: string    // Payment reference number
  createdByUserId: string
  createdUtc: string
}

/**
 * CreatePaymentRequest - Data to record a payment
 */
export interface CreatePaymentRequest {
  amount: number
  paidUtc: string
  method: string
  reference?: string
}

// ==================== Report Types ====================

/**
 * DailyRevenueItem - Revenue for one day
 */
export interface DailyRevenueItem {
  dateUtc: string       // The date
  totalPayments: number // Total revenue that day
  paymentCount: number  // Number of payments
}

/**
 * RevenueSummaryResponse - Total revenue summary
 */
export interface RevenueSummaryResponse {
  startUtc: string
  endUtc: string
  totalPayments: number
  paymentCount: number
}

/**
 * DailyRevenueResponse - Revenue broken down by day
 */
export interface DailyRevenueResponse {
  startUtc: string
  endUtc: string
  days: DailyRevenueItem[]
}

/**
 * SpaceOccupancyItem - How much a space was used
 */
export interface SpaceOccupancyItem {
  spaceId: string
  spaceName: string
  bookingCount: number          // How many bookings
  totalBookedMinutes: number   // Total time booked (in minutes)
}

/**
 * SpaceOccupancyResponse - Occupancy report for all spaces
 */
export interface SpaceOccupancyResponse {
  startUtc: string
  endUtc: string
  totalBookings: number
  spaces: SpaceOccupancyItem[]
}

/**
 * DailySpaceOccupancyItem - Daily occupancy for one space
 */
export interface DailySpaceOccupancyItem {
  dateUtc: string
  spaceId: string
  spaceName: string
  bookingCount: number
  totalBookedMinutes: number
}

/**
 * DailySpaceOccupancyResponse - Daily occupancy for all spaces
 */
export interface DailySpaceOccupancyResponse {
  startUtc: string
  endUtc: string
  items: DailySpaceOccupancyItem[]
}
