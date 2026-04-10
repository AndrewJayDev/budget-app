const BASE_URL = '/api'

let authToken: string | null = null

export function setAuthToken(token: string) {
  authToken = token
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  }
  if (authToken) {
    headers['Authorization'] = `Bearer ${authToken}`
  }

  const res = await fetch(`${BASE_URL}${path}`, { ...options, headers })

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new Error(`${res.status}: ${text}`)
  }

  if (res.status === 204) return undefined as T

  return res.json() as Promise<T>
}

export interface BucketSummaryDto {
  id: string
  name: string
  sortOrder: number
  allocatedMilliunits: number
  activityMilliunits: number
  availableMilliunits: number
}

export interface BucketGroupSummaryDto {
  id: string
  name: string
  sortOrder: number
  buckets: BucketSummaryDto[]
}

export interface MonthBudgetDto {
  year: number
  month: number
  bucketGroups: BucketGroupSummaryDto[]
}

export interface AccountDto {
  id: string
  name: string
  currencyCode: string
  balanceMilliunits: number
  isClosed: boolean
}

export const api = {
  getMonthBudget: (year: number, month: number) =>
    request<MonthBudgetDto>(`/months/${year}-${String(month).padStart(2, '0')}`),

  upsertAllocation: (year: number, month: number, bucketId: string, allocatedMilliunits: number) =>
    request<void>(
      `/months/${year}-${String(month).padStart(2, '0')}/buckets/${bucketId}`,
      {
        method: 'PUT',
        body: JSON.stringify({ allocatedMilliunits }),
      }
    ),

  getAccounts: () => request<AccountDto[]>('/accounts'),
}
