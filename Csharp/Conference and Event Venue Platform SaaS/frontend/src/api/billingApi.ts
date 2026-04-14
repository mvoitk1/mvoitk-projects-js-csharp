// Billing API - Functions for subscription plans and usage
import { apiClient } from './apiClient'
import type { CompanyPlan, PlanUsageResponse } from '../types/apiTypes'

// GET /{companySlug}/billing/plan - Get company's subscription plan and usage
export async function getBillingPlan(companySlug: string): Promise<PlanUsageResponse> {
  const response = await apiClient.get<{
    companyName: string
    companySlug: string
    plan: string
    maxSpaces: number
    currentSpaces: number
    maxBookingsPerMonth: number
    currentBookingsThisMonth: number
    monthStartUtc: string
    monthEndUtc: string
  }>(`/${companySlug}/billing/plan`)

  return {
    companyName: response.companyName,
    companySlug: response.companySlug,
    plan: response.plan as CompanyPlan,
    maxSpaces: response.maxSpaces,
    currentSpaces: response.currentSpaces,
    maxBookingsPerMonth: response.maxBookingsPerMonth,
    currentBookingsThisMonth: response.currentBookingsThisMonth,
    monthStartUtc: response.monthStartUtc,
    monthEndUtc: response.monthEndUtc,
  }
}
