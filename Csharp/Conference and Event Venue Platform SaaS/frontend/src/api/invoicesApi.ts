// Invoices API - Functions to manage invoices (billing documents)
import { api } from './apiClient'
import type { Invoice, InvoiceDetail, InvoiceStatus, Payment, CreatePaymentRequest } from '../types/apiTypes'

// Response type for invoice actions (issue, void, mark-paid, etc.)
export interface InvoiceActionResponse {
  id: string
  status: InvoiceStatus
  issuedUtc?: string
  sentUtc?: string
  paidUtc?: string
  voidedUtc?: string
  isPaid: boolean
  amountPaid: number
  amountDue: number
}

// GET /{companySlug}/invoices - Fetch all invoices
export async function getInvoices(companySlug: string): Promise<Invoice[]> {
  const response = await api.get<Array<{
    id: string
    bookingId: string
    createdUtc: string
    status: string
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
  }>>(`/${companySlug}/invoices`)

  return response.map(invoice => ({
    ...invoice,
    status: invoice.status as Invoice['status'],
  }))
}

// GET /{companySlug}/invoices/{id} - Fetch single invoice with all details
export async function getInvoice(companySlug: string, id: string): Promise<InvoiceDetail> {
  const response = await api.get<{
    id: string
    bookingId: string
    createdUtc: string
    status: string
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
    createdByUserId: string
    items: Array<{
      id: string
      description: string
      quantity: number
      unitPrice: number
      lineTotal: number
    }>
    issuedByUserId?: string
    voidedByUserId?: string
    sentByUserId?: string
    paidByUserId?: string
  }>(`/${companySlug}/invoices/${id}`)

  return {
    ...response,
    status: response.status as InvoiceDetail['status'],
  }
}

// POST /{companySlug}/invoices/{id}/issue - Issue an invoice (send to client)
export async function issueInvoice(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/issue`, {})
}

// POST /{companySlug}/invoices/{id}/mark-sent - Mark invoice as sent
export async function markInvoiceSent(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/mark-sent`, {})
}

// POST /{companySlug}/invoices/{id}/void - Void/cancel an invoice
export async function voidInvoice(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/void`, {})
}

// POST /{companySlug}/invoices/{id}/mark-paid - Mark invoice as paid
export async function markInvoicePaid(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/mark-paid`, {})
}

// GET /{companySlug}/invoices/{invoiceId}/payments - Get all payments for invoice
export async function getInvoicePayments(companySlug: string, invoiceId: string): Promise<Payment[]> {
  return api.get<Payment[]>(`/${companySlug}/invoices/${invoiceId}/payments`)
}

// POST /{companySlug}/invoices/{invoiceId}/payments - Record a new payment
export async function recordInvoicePayment(
  companySlug: string,
  invoiceId: string,
  payload: CreatePaymentRequest
): Promise<Payment> {
  return api.post<Payment>(`/${companySlug}/invoices/${invoiceId}/payments`, payload)
}
