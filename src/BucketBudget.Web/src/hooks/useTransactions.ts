import { useState, useEffect, useCallback } from 'react'
import { getTransactions, type TransactionDto } from '@/lib/api'

export function useTransactions(accountId: string | null) {
  const [transactions, setTransactions] = useState<TransactionDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!accountId) {
      setTransactions([])
      return
    }
    setLoading(true)
    setError(null)
    try {
      const data = await getTransactions({ accountId })
      // Sort by date descending, then by createdAt descending
      data.sort((a, b) => {
        if (b.date !== a.date) return b.date.localeCompare(a.date)
        return b.createdAt.localeCompare(a.createdAt)
      })
      setTransactions(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load transactions')
    } finally {
      setLoading(false)
    }
  }, [accountId])

  useEffect(() => { load() }, [load])

  return { transactions, setTransactions, loading, error, reload: load }
}
