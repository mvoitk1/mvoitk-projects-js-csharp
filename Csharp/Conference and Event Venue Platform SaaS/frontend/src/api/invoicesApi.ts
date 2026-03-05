import { api } from './apiClient'
import type { Invoice, InvoiceDetail, InvoiceStatus, Payment, CreatePaymentRequest } from '../types/apiTypes'

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

export async function issueInvoice(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/issue`, {})
}

export async function markInvoiceSent(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/mark-sent`, {})
}

export async function voidInvoice(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/void`, {})
}

export async function markInvoicePaid(companySlug: string, id: string): Promise<InvoiceActionResponse> {
  return api.post<InvoiceActionResponse>(`/${companySlug}/invoices/${id}/mark-paid`, {})
}

export async function getInvoicePayments(companySlug: string, invoiceId: string): Promise<Payment[]> {
  return api.get<Payment[]>(`/${companySlug}/invoices/${invoiceId}/payments`)
}

export async function recordInvoicePayment(
  companySlug: string,
  invoiceId: string,
  payload: CreatePaymentRequest
): Promise<Payment> {
  return api.post<Payment>(`/${companySlug}/invoices/${invoiceId}/payments`, payload)
}
