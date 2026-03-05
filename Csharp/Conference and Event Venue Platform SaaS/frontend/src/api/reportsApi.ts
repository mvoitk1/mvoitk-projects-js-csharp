// Reports API - Functions for analytics and reporting
import { api } from './apiClient'
import type {
  RevenueSummaryResponse,
  DailyRevenueResponse,
  SpaceOccupancyResponse,
  DailySpaceOccupancyResponse,
} from '../types/apiTypes'

// GET /{companySlug}/reports/revenue - Get total revenue for date range
export async function getRevenueReport(
  companySlug: string,
  fromUtc: string,
  toUtc: string
): Promise<RevenueSummaryResponse> {
  return api.get<RevenueSummaryResponse>(
    `/${companySlug}/reports/revenue?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}`
  )
}

// GET /{companySlug}/reports/revenue/daily - Get daily revenue breakdown
export async function getRevenueDaily(
  companySlug: string,
  fromUtc: string,
  toUtc: string
): Promise<DailyRevenueResponse> {
  return api.get<DailyRevenueResponse>(
    `/${companySlug}/reports/revenue/daily?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}`
  )
}

// GET /{companySlug}/reports/occupancy - Get space occupancy summary
export async function getOccupancyReport(
  companySlug: string,
  fromUtc: string,
  toUtc: string
): Promise<SpaceOccupancyResponse> {
  return api.get<SpaceOccupancyResponse>(
    `/${companySlug}/reports/occupancy?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}`
  )
}

// GET /{companySlug}/reports/occupancy/daily - Get daily occupancy breakdown
export async function getOccupancyDaily(
  companySlug: string,
  fromUtc: string,
  toUtc: string
): Promise<DailySpaceOccupancyResponse> {
  return api.get<DailySpaceOccupancyResponse>(
    `/${companySlug}/reports/occupancy/daily?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}`
  )
}
