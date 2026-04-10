const BASE = '/api'

function getToken(): string | null {
  return localStorage.getItem('token')
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(init?.headers as Record<string, string>),
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${BASE}${path}`, { ...init, headers })
  if (res.status === 204) return undefined as T
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`${res.status}: ${text}`)
  }
  return res.json() as Promise<T>
}

// --- Types ---

export interface AccountDto {
  id: string
  name: string
  currencyCode: string
  balanceMilliunits: number
  isClosed: boolean
  createdAt: string
  updatedAt: string
}

export interface TransactionDto {
  id: string
  accountId: string
  bucketId: string | null
  payee: string
  amountMilliunits: number
  date: string
  memo: string | null
  isCleared: boolean
  createdAt: string
  updatedAt: string
}

export interface BucketDto {
  id: string
  bucketGroupId: string
  name: string
  sortOrder: number
}

export interface BucketGroupDto {
  id: string
  name: string
  buckets: BucketDto[]
}

// --- Auth ---

export async function login(username: string, password: string): Promise<{ token: string }> {
  return request<{ token: string }>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
}

// --- Accounts ---

export async function getAccounts(includeClosed = false): Promise<AccountDto[]> {
  return request<AccountDto[]>(`/accounts?includeClosed=${includeClosed}`)
}

export async function getAccount(id: string): Promise<AccountDto> {
  return request<AccountDto>(`/accounts/${id}`)
}

export async function createAccount(data: { name: string; currencyCode: string }): Promise<{ id: string }> {
  return request<{ id: string }>('/accounts', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateAccount(id: string, data: { name: string; currencyCode: string; isClosed: boolean }): Promise<void> {
  return request<void>(`/accounts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function deleteAccount(id: string): Promise<void> {
  return request<void>(`/accounts/${id}`, { method: 'DELETE' })
}

// --- Transactions ---

export interface GetTransactionsParams {
  accountId?: string
  bucketId?: string
  from?: string
  to?: string
}

export async function getTransactions(params: GetTransactionsParams = {}): Promise<TransactionDto[]> {
  const q = new URLSearchParams()
  if (params.accountId) q.set('accountId', params.accountId)
  if (params.bucketId) q.set('bucketId', params.bucketId)
  if (params.from) q.set('from', params.from)
  if (params.to) q.set('to', params.to)
  return request<TransactionDto[]>(`/transactions?${q}`)
}

export interface CreateTransactionData {
  accountId: string
  bucketId?: string | null
  payee: string
  amountMilliunits: number
  date: string
  memo?: string | null
  isCleared: boolean
}

export async function createTransaction(data: CreateTransactionData): Promise<{ id: string }> {
  return request<{ id: string }>('/transactions', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function bulkCreateTransactions(transactions: CreateTransactionData[]): Promise<string[]> {
  return request<string[]>('/transactions/bulk', {
    method: 'POST',
    body: JSON.stringify({ transactions }),
  })
}

export interface UpdateTransactionData {
  bucketId?: string | null
  payee: string
  amountMilliunits: number
  date: string
  memo?: string | null
  isCleared: boolean
}

export async function updateTransaction(id: string, data: UpdateTransactionData): Promise<void> {
  return request<void>(`/transactions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function bulkUpdateTransactions(updates: Array<{ id: string } & UpdateTransactionData>): Promise<void> {
  return request<void>('/transactions/bulk', {
    method: 'PUT',
    body: JSON.stringify({ transactions: updates }),
  })
}

export async function deleteTransaction(id: string): Promise<void> {
  return request<void>(`/transactions/${id}`, { method: 'DELETE' })
}

// --- Budget / Buckets ---
// The GET /months/:month endpoint returns bucket groups with buckets

export interface MonthBudgetDto {
  year: number
  month: number
  bucketGroups: BucketGroupDto[]
}

export async function getMonthBudget(year: number, month: number): Promise<MonthBudgetDto> {
  const m = String(month).padStart(2, '0')
  return request<MonthBudgetDto>(`/months/${year}-${m}`)
}
