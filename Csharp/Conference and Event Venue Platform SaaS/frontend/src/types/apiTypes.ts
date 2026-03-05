// ==================== Auth Types ====================

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  userId: string
  email: string
}

export interface RegisterRequest {
  email: string
  password: string
  mode: 'Owner' | 'User'
  companyName?: string    // Required when mode === 'Owner'
  companySlug: string     // Required for both modes
  role?: string           // Optional, only honored internally
}

export interface RegisterResponse {
  userId: string
  email: string
  companySlug: string | null
  membershipRole: string | null
}

// ==================== Billing Types ====================

export type CompanyPlan = 'Free' | 'Starter' | 'Professional' | 'Enterprise'

export interface PlanUsageResponse {
  companyName: string
  companySlug: string
  plan: CompanyPlan
  maxSpaces: number
  currentSpaces: number
  maxBookingsPerMonth: number
  currentBookingsThisMonth: number
  monthStartUtc: string
  monthEndUtc: string
}

// ==================== API Error Types ====================

export interface ApiErrorResponse {
  code: string
  error: string
  details?: Record<string, string[]>
}

export class ApiError extends Error {
  constructor(
    public code: string,
    message: string,
    public details?: Record<string, string[]>,
    public statusCode?: number
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

// ==================== Error Code Mapping ====================

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

export function getErrorMessage(code: string): string {
  return ERROR_CODE_MESSAGES[code] || 'An unexpected error occurred'
}

// ==================== User Types ====================

export interface User {
  id: string
  email: string
  companySlug: string
}

// ==================== Company Types ====================

export interface Company {
  id: string
  name: string
  slug: string
  plan: CompanyPlan
}

// ==================== Space Types ====================

export interface Space {
  id: string
  name: string
  capacity: number
  notes: string | null
  isActive: boolean
}

export interface CreateSpaceRequest {
  name: string
  capacity: number
  notes?: string
}

export interface UpdateSpaceRequest {
  name: string
  capacity: number
  notes?: string
}

// ==================== Client Types ====================

export interface Client {
  id: string
  name: string
  email?: string
  notes?: string
  companyId: string
  createdUtc?: string
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

export type BookingStatus = 'Pending' | 'Confirmed'

export interface Booking {
  id: string
  clientId: string
  title: string
  startUtc: string
  endUtc: string
  attendeeCount: number
  isCancelled: boolean
  spaceIds: string[]
  totalAmount: number
  spaceConfigurationId?: string
  status: BookingStatus
  cancelledUtc?: string
  cancelReason?: string
  createdByUserId?: string
  confirmedByUserId?: string
  cancelledByUserId?: string
}

export interface BookingDetails {
  id: string
  clientId: string
  clientName: string
  title: string
  startUtc: string
  endUtc: string
  attendeeCount: number
  isCancelled: boolean
  totalAmount: number
  spaceConfigurationId?: string
  spaces: SpaceSummary[]
  status: BookingStatus
  cancelledUtc?: string
  cancelReason?: string
  createdByUserId?: string
  confirmedByUserId?: string
  cancelledByUserId?: string
}

export interface SpaceSummary {
  id: string
  name: string
}

export interface CreateBookingRequest {
  clientId: string
  title: string
  startUtc: string
  endUtc: string
  attendeeCount: number
  spaceIds: string[]
  spaceConfigurationId?: string
}

export interface BookingConflictResponse {
  error: string
  conflictingBookingIds: string[]
  conflictingSpaceIds: string[]
}

// ==================== Invoice Types ====================

export type InvoiceStatus = 'Draft' | 'Issued' | 'Void' | 'Sent' | 'Paid'

export interface InvoiceItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface Invoice {
  id: string
  bookingId: string
  createdUtc: string
  status: InvoiceStatus
  currency: string
  subtotalAmount: number
  invoiceNumber: number
  invoiceNumberText: string
  issuedUtc?: string
  voidedUtc?: string
  sentUtc?: string
  amountPaid: number
  amountDue: number
  isPaid: boolean
  paidUtc?: string
}

export interface InvoiceDetail extends Invoice {
  createdByUserId: string
  items: InvoiceItem[]
  issuedByUserId?: string
  voidedByUserId?: string
  sentByUserId?: string
  paidByUserId?: string
}

export interface Payment {
  id: string
  invoiceId: string
  amount: number
  paidUtc: string
  method: string
  reference?: string
  createdByUserId: string
  createdUtc: string
}

export interface CreatePaymentRequest {
  amount: number
  paidUtc: string
  method: string
  reference?: string
}

// ==================== Report Types ====================

export interface DailyRevenueItem {
  dateUtc: string
  totalPayments: number
  paymentCount: number
}

export interface RevenueSummaryResponse {
  startUtc: string
  endUtc: string
  totalPayments: number
  paymentCount: number
}

export interface DailyRevenueResponse {
  startUtc: string
  endUtc: string
  days: DailyRevenueItem[]
}

export interface SpaceOccupancyItem {
  spaceId: string
  spaceName: string
  bookingCount: number
  totalBookedMinutes: number
}

export interface SpaceOccupancyResponse {
  startUtc: string
  endUtc: string
  totalBookings: number
  spaces: SpaceOccupancyItem[]
}

export interface DailySpaceOccupancyItem {
  dateUtc: string
  spaceId: string
  spaceName: string
  bookingCount: number
  totalBookedMinutes: number
}

export interface DailySpaceOccupancyResponse {
  startUtc: string
  endUtc: string
  items: DailySpaceOccupancyItem[]
}