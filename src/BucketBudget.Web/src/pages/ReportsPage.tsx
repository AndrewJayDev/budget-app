import { useState, useEffect, useMemo } from 'react'
import {
  BarChart, Bar, LineChart, Line, AreaChart, Area,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  PieChart, Pie, Cell,
} from 'recharts'
import { getTransactions, getAccounts, type TransactionDto, type AccountDto } from '@/lib/api'
import { useBuckets } from '@/hooks/useBuckets'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { cn, formatMilliunits } from '@/lib/utils'

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4', '#f97316', '#84cc16']

// --- helpers ---

function isoMonth(date: string): string {
  return date.slice(0, 7) // "YYYY-MM"
}

function monthLabel(ym: string): string {
  const [y, m] = ym.split('-')
  const d = new Date(Number(y), Number(m) - 1, 1)
  return d.toLocaleString('default', { month: 'short', year: '2-digit' })
}

function monthsBetween(fromYm: string, toYm: string): string[] {
  const months: string[] = []
  const [fy, fm] = fromYm.split('-').map(Number)
  const [ty, tm] = toYm.split('-').map(Number)
  let y = fy, m = fm
  while (y < ty || (y === ty && m <= tm)) {
    months.push(`${y}-${String(m).padStart(2, '0')}`)
    m++
    if (m > 12) { m = 1; y++ }
  }
  return months
}

function fmtAmount(n: number, currency: string): string {
  // Reuse the locale-aware formatting from utils, converting to milliunits
  return formatMilliunits(Math.round(n * 1000), currency)
}

function startOfYear(): string {
  return `${new Date().getFullYear()}-01-01`
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

// --- component ---

export function ReportsPage() {
  const [accounts, setAccounts] = useState<AccountDto[]>([])
  const [allTransactions, setAllTransactions] = useState<TransactionDto[]>([])
  const [loading, setLoading] = useState(true)

  const [dateFrom, setDateFrom] = useState(startOfYear)
  const [dateTo, setDateTo] = useState(today)
  const [accountId, setAccountId] = useState<string>('all')
  const [currency, setCurrency] = useState<string>('USD')
  const [chartType, setChartType] = useState<'bar' | 'pie'>('bar')

  const { buckets } = useBuckets()
  const bucketMap = useMemo(() => new Map(buckets.map(b => [b.id, b.name])), [buckets])

  // Derive unique currencies from accounts
  const currencies = useMemo(() => [...new Set(accounts.map(a => a.currencyCode))], [accounts])

  // When accounts load, pick the first currency
  useEffect(() => {
    if (currencies.length > 0 && !currencies.includes(currency)) {
      setCurrency(currencies[0])
    }
  }, [currencies]) // eslint-disable-line react-hooks/exhaustive-deps

  // Fetch data on mount or account filter change
  useEffect(() => {
    let cancelled = false
    setLoading(true)

    async function load() {
      try {
        const [accts, txns] = await Promise.all([
          getAccounts(true),
          getTransactions(accountId !== 'all' ? { accountId } : {}),
        ])
        if (cancelled) return
        setAccounts(accts)
        setAllTransactions(txns)
      } catch (e) {
        console.error('Reports fetch error:', e)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    load()
    return () => { cancelled = true }
  }, [accountId])

  // Filtered accounts by currency
  const filteredAccounts = useMemo(
    () => accounts.filter(a => accountId === 'all' ? a.currencyCode === currency : a.id === accountId),
    [accounts, accountId, currency]
  )
  const filteredAccountIds = useMemo(() => new Set(filteredAccounts.map(a => a.id)), [filteredAccounts])

  // When specific account selected, force currency to match
  const effectiveCurrency = useMemo(() => {
    if (accountId !== 'all') {
      return accounts.find(a => a.id === accountId)?.currencyCode ?? currency
    }
    return currency
  }, [accountId, accounts, currency])

  // Date-range filtered transactions in the matching currency accounts
  const filteredTxns = useMemo(() =>
    allTransactions.filter(t =>
      filteredAccountIds.has(t.accountId) &&
      t.date >= dateFrom &&
      t.date <= dateTo
    ),
    [allTransactions, filteredAccountIds, dateFrom, dateTo]
  )

  // All transactions for those accounts (no date filter) — for net worth
  const allFilteredTxns = useMemo(() =>
    allTransactions.filter(t => filteredAccountIds.has(t.accountId)),
    [allTransactions, filteredAccountIds]
  )

  // ---- Chart 1: Spending by Bucket ----
  const spendingByBucket = useMemo(() => {
    const map = new Map<string, number>()
    for (const t of filteredTxns) {
      if (t.amountMilliunits >= 0) continue
      const key = (t.bucketId && bucketMap.get(t.bucketId)) ?? 'Uncategorized'
      map.set(key, (map.get(key) ?? 0) + Math.abs(t.amountMilliunits) / 1000)
    }
    return [...map.entries()]
      .sort((a, b) => b[1] - a[1])
      .slice(0, 12)
      .map(([name, value]) => ({ name, value: Math.round(value) }))
  }, [filteredTxns, bucketMap])

  // ---- Chart 2: Monthly Spending Trend ----
  const monthlySpending = useMemo(() => {
    const fromYm = isoMonth(dateFrom)
    const toYm = isoMonth(dateTo)
    const months = monthsBetween(fromYm, toYm)
    const map = new Map<string, number>()
    for (const t of filteredTxns) {
      if (t.amountMilliunits >= 0) continue
      const ym = isoMonth(t.date)
      map.set(ym, (map.get(ym) ?? 0) + Math.abs(t.amountMilliunits) / 1000)
    }
    return months.map(ym => ({ month: monthLabel(ym), value: Math.round(map.get(ym) ?? 0) }))
  }, [filteredTxns, dateFrom, dateTo])

  // ---- Chart 3: Income vs Expense ----
  const incomeVsExpense = useMemo(() => {
    const fromYm = isoMonth(dateFrom)
    const toYm = isoMonth(dateTo)
    const months = monthsBetween(fromYm, toYm)
    const incomeMap = new Map<string, number>()
    const expenseMap = new Map<string, number>()
    for (const t of filteredTxns) {
      const ym = isoMonth(t.date)
      if (t.amountMilliunits > 0) {
        incomeMap.set(ym, (incomeMap.get(ym) ?? 0) + t.amountMilliunits / 1000)
      } else {
        expenseMap.set(ym, (expenseMap.get(ym) ?? 0) + Math.abs(t.amountMilliunits) / 1000)
      }
    }
    return months.map(ym => ({
      month: monthLabel(ym),
      income: Math.round(incomeMap.get(ym) ?? 0),
      expense: Math.round(expenseMap.get(ym) ?? 0),
    }))
  }, [filteredTxns, dateFrom, dateTo])

  // ---- Chart 4: Net Worth Over Time ----
  // For each month from earliest transaction to today, compute total balance of filtered accounts
  const netWorthData = useMemo(() => {
    if (filteredAccounts.length === 0 || allFilteredTxns.length === 0) return []

    // Get the total current balance of filtered accounts
    const currentBalance = filteredAccounts
      .filter(a => !a.isClosed)
      .reduce((sum, a) => sum + a.balanceMilliunits, 0) / 1000

    // Determine the month range: from earliest txn to today
    const dates = allFilteredTxns.map(t => t.date)
    const minDate = dates.reduce((a, b) => a < b ? a : b)
    const nowYm = isoMonth(today())
    const months = monthsBetween(isoMonth(minDate), nowYm)

    if (months.length === 0) return []

    // For each month M: balance = currentBalance - sum(txns with date > last day of M)
    // Equivalently: for each month in reverse order, subtract transactions that fall after it
    // Build cumulative sum from the end
    const monthlyNetAmounts = new Map<string, number>()
    for (const t of allFilteredTxns) {
      const ym = isoMonth(t.date)
      monthlyNetAmounts.set(ym, (monthlyNetAmounts.get(ym) ?? 0) + t.amountMilliunits / 1000)
    }

    // Walk months from most recent to oldest, accumulating what's been subtracted
    const result: { month: string; netWorth: number }[] = []
    let subtracted = 0
    const reversedMonths = [...months].reverse()
    for (const ym of reversedMonths) {
      // balance at end of this month = currentBalance - transactions AFTER this month
      // "after" = in months that came after ym (which we've already processed in reverse)
      const netWorth = Math.round(currentBalance - subtracted)
      result.unshift({ month: monthLabel(ym), netWorth })
      subtracted += (monthlyNetAmounts.get(ym) ?? 0)
    }

    return result
  }, [filteredAccounts, allFilteredTxns])

  const hasTxns = allTransactions.length > 0

  return (
    <div className="flex-1 flex flex-col overflow-hidden bg-gray-50">
      {/* Filter Bar */}
      <div className="bg-white border-b px-6 py-3 flex items-center gap-4 flex-wrap shrink-0">
        <h2 className="text-lg font-semibold text-gray-900 mr-2">Reports</h2>

        {/* Date range */}
        <div className="flex items-center gap-2">
          <label className="text-xs text-gray-500">From</label>
          <input
            type="date"
            value={dateFrom}
            max={dateTo}
            onChange={e => setDateFrom(e.target.value)}
            className="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <label className="text-xs text-gray-500">To</label>
          <input
            type="date"
            value={dateTo}
            min={dateFrom}
            max={today()}
            onChange={e => setDateTo(e.target.value)}
            className="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Account filter */}
        <div className="flex items-center gap-2">
          <label className="text-xs text-gray-500">Account</label>
          <Select value={accountId} onValueChange={setAccountId}>
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Accounts</SelectItem>
              {accounts.filter(a => !a.isClosed).map(a => (
                <SelectItem key={a.id} value={a.id}>{a.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Currency filter — only when showing all accounts */}
        {accountId === 'all' && currencies.length > 1 && (
          <div className="flex items-center gap-2">
            <label className="text-xs text-gray-500">Currency</label>
            <Select value={currency} onValueChange={setCurrency}>
              <SelectTrigger className="w-24">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {currencies.map(c => (
                  <SelectItem key={c} value={c}>{c}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}
      </div>

      {/* Charts */}
      <div className="flex-1 overflow-y-auto p-6">
        {loading ? (
          <div className="flex items-center justify-center h-64 text-gray-400">Loading reports…</div>
        ) : !hasTxns ? (
          <div className="flex items-center justify-center h-64 text-gray-400">
            No transactions found. Add transactions to see analytics.
          </div>
        ) : (
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            {/* Chart 1: Spending by Bucket */}
            <ChartCard
              title="Spending by Bucket"
              subtitle={`${dateFrom} – ${dateTo}`}
              extra={
                <div className="flex gap-1">
                  <button
                    onClick={() => setChartType('bar')}
                    className={cn('text-xs px-2 py-0.5 rounded', chartType === 'bar' ? 'bg-blue-100 text-blue-700' : 'text-gray-400 hover:text-gray-600')}
                  >Bar</button>
                  <button
                    onClick={() => setChartType('pie')}
                    className={cn('text-xs px-2 py-0.5 rounded', chartType === 'pie' ? 'bg-blue-100 text-blue-700' : 'text-gray-400 hover:text-gray-600')}
                  >Pie</button>
                </div>
              }
            >
              {spendingByBucket.length === 0 ? (
                <EmptyChart />
              ) : chartType === 'bar' ? (
                <ResponsiveContainer width="100%" height={280}>
                  <BarChart data={spendingByBucket} layout="vertical" margin={{ left: 8, right: 24, top: 4, bottom: 4 }}>
                    <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                    <XAxis type="number" tickFormatter={v => fmtAmount(v, effectiveCurrency)} tick={{ fontSize: 11 }} />
                    <YAxis type="category" dataKey="name" width={110} tick={{ fontSize: 11 }} />
                    <Tooltip formatter={(v) => typeof v === 'number' ? fmtAmount(v, effectiveCurrency) : String(v)} />
                    <Bar dataKey="value" name="Spending" fill="#3b82f6" radius={[0, 3, 3, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <PieChart>
                    <Pie
                      data={spendingByBucket}
                      dataKey="value"
                      nameKey="name"
                      cx="50%"
                      cy="50%"
                      outerRadius={100}
                      label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`}
                      labelLine={false}
                    >
                      {spendingByBucket.map((_, i) => (
                        <Cell key={i} fill={COLORS[i % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(v) => typeof v === 'number' ? fmtAmount(v, effectiveCurrency) : String(v)} />
                  </PieChart>
                </ResponsiveContainer>
              )}
            </ChartCard>

            {/* Chart 2: Monthly Spending Trend */}
            <ChartCard title="Monthly Spending" subtitle="Expenses over time">
              {monthlySpending.every(d => d.value === 0) ? (
                <EmptyChart />
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <LineChart data={monthlySpending} margin={{ left: 8, right: 16, top: 4, bottom: 4 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                    <YAxis tickFormatter={v => fmtAmount(v, effectiveCurrency)} tick={{ fontSize: 11 }} width={70} />
                    <Tooltip formatter={(v) => typeof v === 'number' ? fmtAmount(v, effectiveCurrency) : String(v)} />
                    <Line type="monotone" dataKey="value" name="Spending" stroke="#3b82f6" strokeWidth={2} dot={monthlySpending.length <= 12} activeDot={{ r: 5 }} />
                  </LineChart>
                </ResponsiveContainer>
              )}
            </ChartCard>

            {/* Chart 3: Income vs Expense */}
            <ChartCard title="Income vs Expense" subtitle="Monthly comparison">
              {incomeVsExpense.every(d => d.income === 0 && d.expense === 0) ? (
                <EmptyChart />
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <BarChart data={incomeVsExpense} margin={{ left: 8, right: 16, top: 4, bottom: 4 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                    <YAxis tickFormatter={v => fmtAmount(v, effectiveCurrency)} tick={{ fontSize: 11 }} width={70} />
                    <Tooltip formatter={(v) => typeof v === 'number' ? fmtAmount(v, effectiveCurrency) : String(v)} />
                    <Legend />
                    <Bar dataKey="income" name="Income" fill="#10b981" radius={[3, 3, 0, 0]} />
                    <Bar dataKey="expense" name="Expense" fill="#ef4444" radius={[3, 3, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </ChartCard>

            {/* Chart 4: Net Worth Over Time */}
            <ChartCard title="Net Worth" subtitle="Account balance over time">
              {netWorthData.length < 2 ? (
                <EmptyChart message="Not enough history to show net worth trend" />
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <AreaChart data={netWorthData} margin={{ left: 8, right: 16, top: 4, bottom: 4 }}>
                    <defs>
                      <linearGradient id="netWorthGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.25} />
                        <stop offset="95%" stopColor="#3b82f6" stopOpacity={0.03} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                    <YAxis tickFormatter={v => fmtAmount(v, effectiveCurrency)} tick={{ fontSize: 11 }} width={80} />
                    <Tooltip formatter={(v) => typeof v === 'number' ? fmtAmount(v, effectiveCurrency) : String(v)} />
                    <Area
                      type="monotone"
                      dataKey="netWorth"
                      name="Net Worth"
                      stroke="#3b82f6"
                      strokeWidth={2}
                      fill="url(#netWorthGrad)"
                      dot={false}
                    />
                  </AreaChart>
                </ResponsiveContainer>
              )}
            </ChartCard>
          </div>
        )}
      </div>
    </div>
  )
}

function ChartCard({ title, subtitle, extra, children }: {
  title: string
  subtitle?: string
  extra?: React.ReactNode
  children: React.ReactNode
}) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 shadow-sm p-4">
      <div className="flex items-start justify-between mb-3">
        <div>
          <h3 className="text-sm font-semibold text-gray-900">{title}</h3>
          {subtitle && <p className="text-xs text-gray-400 mt-0.5">{subtitle}</p>}
        </div>
        {extra}
      </div>
      {children}
    </div>
  )
}

function EmptyChart({ message = 'No data for this period' }: { message?: string }) {
  return (
    <div className="flex items-center justify-center h-[280px] text-sm text-gray-400">{message}</div>
  )
}
